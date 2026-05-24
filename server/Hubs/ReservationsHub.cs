using System.Collections.Concurrent;
using System.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using server.Data;
using server.Data.Entities;
using server.Dto.Mappers;
using server.Dto.Requests;
using server.Infrastructure;
using server.Services;

namespace server.Hubs;

public sealed class ReservationsHub(
	IAuthService auth,
	AppDbContext db,
	ReservationCacheService reservationCache,
	IHubContext<ReservationsHub> hubContext,
	IDbLoggerService dbLogger,
	IAppSettingsService appSettings
) : Hub {
	public static readonly ConcurrentDictionary<string, byte> ConnectedIds = [];
	private static readonly object ConnectedStatusLock = new();
	private static readonly TimeSpan ConnectedStatusInterval = TimeSpan.FromSeconds(1);
	private static DateTime LastConnectedStatusSentUtc = DateTime.MinValue;
	private static bool ConnectedStatusQueued;

	public override async Task OnConnectedAsync() {
		ConnectedIds.TryAdd(Context.ConnectionId, 0);
		var account = await auth.ReAuthAsync();
		// anonymum neposilame profily, prihlasenej clovek je muze videt
		await Groups.AddToGroupAsync(Context.ConnectionId, account == null ? "anonymous" : "authenticated");
		await Clients.Caller.SendAsync("ReceiveReservations", new { reservations = await FetchReservations() });
		await QueueConnectedStatusBroadcast();
		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception) {
		ConnectedIds.TryRemove(Context.ConnectionId, out _);
		await QueueConnectedStatusBroadcast();
		await base.OnDisconnectedAsync(exception);
	}

	public async Task Reserve(ReserveRequest request) {
		var ct = Context.ConnectionAborted;
		var account = await auth.ReAuthAsync(ct);
		if (account == null) {
			await SendError("Nejste přihlášený.");
			return;
		}
		
		if (!await appSettings.AreReservationsEnabledRightNowAsync(ct)) {
			await SendError("Rezervace jsou momentálně uzavřené.");
			return;
		}

		if (!account.EnableReservations) {
			await SendError("Nemáte povolené rezervace.");
			return;
		}

		if (request.Type != "computer" && request.Type != "room") {
			await SendError("Neplatný typ rezervace.");
			return;
		}

		try {
			// serializable, at se dva rychly lidi nenacpou na stejny misto naraz
			await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

			var accountEntity = await db.Accounts.FirstAsync(a => a.Id == account.Id, ct);
			var existingReservation = await db.ReservationsEf().FirstOrDefaultAsync(r => r.AccountId == account.Id, ct);
			Reservation? previousReservation = existingReservation;
			Reservation? reservation = null;
			string? reservedTarget = null;

			switch (request.Type) {
				case "computer": {
					var computer = await db.ComputersEf().FirstOrDefaultAsync(c => c.Id == request.Id, ct);
					if (computer == null) {
						await SendError("Počítač neexistuje nebo není dostupný.");
						return;
					}

					if (computer.IsTeachersComputer && account.AccountType < AccountType.Teacher) {
						await SendError("Tento počítač je vyhrazený pro učitele.");
						return;
					}

					var reservationExists = await db.ComputerReservations
						.AnyAsync(r => r.Computer.Id == request.Id && r.AccountId != account.Id, ct);

					if (reservationExists) {
						await SendError("Toto místo je již rezervované.");
						return;
					}

					if (existingReservation != null) db.Reservations.Remove(existingReservation);

					reservation = new ComputerReservation {
						Id = Guid.CreateVersion7(),
						Account = accountEntity,
						AccountId = account.Id,
						Computer = computer,
						Note = null,
					};
					db.Reservations.Add(reservation);
					reservedTarget = computer.Id;
					break;
				}

				case "room": {
					var room = await db.RoomsEf().FirstOrDefaultAsync(r => r.Id == request.Id, ct);
					if (room == null) {
						await SendError("Místnost neexistuje nebo není dostupná.");
						return;
					}

					var reservationCount = await db.RoomReservations
						.CountAsync(r => r.Room.Id == request.Id && r.AccountId != account.Id, ct);

					if (reservationCount >= room.Capacity) {
						await SendError("Toto místo je již rezervované.");
						return;
					}

					if (existingReservation != null) db.Reservations.Remove(existingReservation);

					reservation = new RoomReservation {
						Id = Guid.CreateVersion7(),
						Account = accountEntity,
						AccountId = account.Id,
						Room = room,
						Note = null,
					};
					db.Reservations.Add(reservation);
					reservedTarget = room.Id;
					break;
				}
			}


			await db.SaveChangesAsync(ct);
			await transaction.CommitAsync(ct);
			// cache drzime stejne jako klienty - jen delta update, zadnej full reload po kazdy zmene
			await reservationCache.ApplyReservationChangeAsync(previousReservation, reservation);

			if (reservation != null && !string.IsNullOrWhiteSpace(reservedTarget)) {
				await dbLogger.LogInfoAsync(
					$"Uživatel {FormatAccount(account)} rezervoval {reservedTarget}.",
					"reservation", ct
					);
			}

			await BroadcastReservationsChanged("booked", "Rezervace byla upravena.", previousReservation, reservation);
		} catch (DbUpdateException ex) when (IsConcurrentReservationWrite(ex)) {
			await SendError("Rezervaci se nepodařilo uložit, protože ji mezitím změnil někdo jiný. Zkuste to prosím znovu.");
			return;
		} catch (PostgresException ex) when (IsConcurrentReservationWrite(ex)) {
			await SendError("Rezervaci se nepodařilo uložit. Zkuste to prosím znovu.");
			return;
		}

	}

	public async Task Unbook() {
		var ct = Context.ConnectionAborted;
		var account = await auth.ReAuthAsync(ct);
		if (account == null) {
			await SendError("Nejste přihlášený.");
			return;
		}

		if (!await appSettings.AreReservationsEnabledRightNowAsync(ct)) {
			await SendError("Rezervace jsou momentálně uzavřené.");
			return;
		}

		var existingReservation = await db.ReservationsEf().FirstOrDefaultAsync(r => r.AccountId == account.Id, ct);
		if (existingReservation == null) {
			await SendError("Nemáte žádnou rezervaci.");
			return;
		}

		db.Reservations.Remove(existingReservation);
		await db.SaveChangesAsync(ct);
		await reservationCache.ApplyReservationChangeAsync(existingReservation, null);

		await dbLogger.LogInfoAsync(
			$"Uživatel {FormatAccount(account)} zrušil rezervaci.",
			"reservation"
		);

		await BroadcastReservationsChanged("unbooked", "Rezervace byla smazána.", existingReservation, null);
	}

	// helpery metodiky
	private async Task<List<object>> FetchReservations() {
		var account = await auth.ReAuthAsync();
		return await reservationCache.GetReservationsAsync(account != null);
	}

	private Task SendError(string message) {
		return Clients.Caller.SendAsync("ReceiveError", new { message });
	}

	private Task QueueConnectedStatusBroadcast() {
		var delay = TimeSpan.Zero;
		var sendNow = false;
		var queuedNow = false;

		lock (ConnectedStatusLock) {
			var nowUtc = DateTime.UtcNow;
			var elapsed = nowUtc - LastConnectedStatusSentUtc;

			if (elapsed >= ConnectedStatusInterval && !ConnectedStatusQueued) {
				LastConnectedStatusSentUtc = nowUtc;
				sendNow = true;
			} else if (!ConnectedStatusQueued) {
				// kdyz se pripojuje/odpojuje hromada lidi, poslu max jednu zpravu za sekundu
				ConnectedStatusQueued = true;
				queuedNow = true;
				delay = ConnectedStatusInterval - elapsed;
			}
		}

		if (sendNow) return BroadcastConnectedStatus();
		if (queuedNow) _ = BroadcastConnectedStatusLater(delay);

		return Task.CompletedTask;
	}

	private async Task BroadcastConnectedStatusLater(TimeSpan delay) {
		if (delay > TimeSpan.Zero) await Task.Delay(delay);

		lock (ConnectedStatusLock) {
			LastConnectedStatusSentUtc = DateTime.UtcNow;
			ConnectedStatusQueued = false;
		}

		await BroadcastConnectedStatus();
	}

	private Task BroadcastConnectedStatus() {
		return hubContext.Clients.All.SendAsync("ReceiveStatus", new { connectedIds = ConnectedIds.Count });
	}

	private Task BroadcastReservationsChanged(string action, string message, Reservation? previousReservation, Reservation? reservation) {
		// stejna zprava, ale dve verze dat: s profilama a anonymni
		return Task.WhenAll(
			Clients.Group("authenticated").SendAsync("ReservationsChanged", new {
				action,
				message,
				previousReservation = previousReservation?.ToDto(),
				reservation = reservation?.ToDto()
			}),
			Clients.Group("anonymous").SendAsync("ReservationsChanged", new {
				action,
				message,
				previousReservation = previousReservation?.ToAnonymousDto(),
				reservation = reservation?.ToAnonymousDto()
			})
		);
	}

	private static bool IsConcurrentReservationWrite(Exception exception) {
		return exception is PostgresException {
			SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.UniqueViolation
		} || exception.InnerException is not null && IsConcurrentReservationWrite(exception.InnerException);
	}

	private static string FormatAccount(Account account) {
		return $"{account.FirstName} {account.LastName} ({account.Email})";
	}
}
