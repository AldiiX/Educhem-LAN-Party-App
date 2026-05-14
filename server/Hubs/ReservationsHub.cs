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
	AppDbContext db
) : Hub {
	public static readonly ConcurrentDictionary<string, byte> ConnectedIds = [];

	public override async Task OnConnectedAsync() {
		ConnectedIds.TryAdd(Context.ConnectionId, 0);
		var account = await auth.ReAuthAsync();
		await Groups.AddToGroupAsync(Context.ConnectionId, account == null ? "anonymous" : "authenticated");
		await Clients.Caller.SendAsync("ReceiveReservations", new { reservations = await FetchReservations() });
		await Clients.All.SendAsync("ReceiveStatus", new { connectedIds = ConnectedIds.Count });
		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception) {
		ConnectedIds.TryRemove(Context.ConnectionId, out _);
		await Clients.All.SendAsync("ReceiveStatus", new { connectedIds = ConnectedIds.Count });
		await base.OnDisconnectedAsync(exception);
	}

	public async Task Reserve(ReserveRequest request) {
		var account = await auth.ReAuthAsync();
		if (account == null) {
			await SendError("Nejste přihlášený.");
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
			await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

			var accountEntity = await db.Accounts.FirstAsync(a => a.Id == account.Id);
			var existingReservation = await db.ReservationsEf().FirstOrDefaultAsync(r => r.AccountId == account.Id);
			Reservation? previousReservation = existingReservation;
			Reservation? reservation = null;

			switch (request.Type) {
				case "computer": {
					var computer = await db.ComputersEf().FirstOrDefaultAsync(c => c.Id == request.Id);
					if (computer == null) {
						await SendError("Počítač neexistuje nebo není dostupný.");
						return;
					}

					if (computer.IsTeachersComputer && account.AccountType < AccountType.Teacher) {
						await SendError("Tento počítač je vyhrazený pro učitele.");
						return;
					}

					var reservationExists = await db.ComputerReservations
						.AnyAsync(r => r.Computer.Id == request.Id && r.AccountId != account.Id);

					if (reservationExists) {
						await SendError("Toto místo je již rezervované.");
						return;
					}

					if (existingReservation != null) db.Reservations.Remove(existingReservation);

					reservation = new ComputerReservation {
						Account = accountEntity,
						AccountId = account.Id,
						Computer = computer,
						Note = null,
					};
					db.Reservations.Add(reservation);
					break;
				}

				case "room": {
					var room = await db.RoomsEf().FirstOrDefaultAsync(r => r.Id == request.Id);
					if (room == null) {
						await SendError("Místnost neexistuje nebo není dostupná.");
						return;
					}

					var reservationCount = await db.RoomReservations
						.CountAsync(r => r.Room.Id == request.Id && r.AccountId != account.Id);

					if (reservationCount >= room.Capacity) {
						await SendError("Toto místo je již rezervované.");
						return;
					}

					if (existingReservation != null) db.Reservations.Remove(existingReservation);

					reservation = new RoomReservation {
						Account = accountEntity,
						AccountId = account.Id,
						Room = room,
						Note = null,
					};
					db.Reservations.Add(reservation);
					break;
				}
			}

			await db.SaveChangesAsync();
			await transaction.CommitAsync();

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
		var account = await auth.ReAuthAsync();
		if (account == null) {
			await SendError("Nejste přihlášený.");
			return;
		}

		var existingReservation = await db.ReservationsEf().FirstOrDefaultAsync(r => r.AccountId == account.Id);
		if (existingReservation == null) {
			await SendError("Nemáte žádnou rezervaci.");
			return;
		}

		db.Reservations.Remove(existingReservation);
		await db.SaveChangesAsync();
		await BroadcastReservationsChanged("unbooked", "Rezervace byla smazána.", existingReservation, null);
	}

	// helpery metodiky
	private async Task<List<object>> FetchReservations() {
		var account = await auth.ReAuthAsync();
		var r = await db.ReservationsEf().AsNoTracking().ToListAsync();

		return account == null
			? r.Select(x => x.ToAnonymousDto()).Cast<object>().ToList()
			: r.Select(x => x.ToDto()).Cast<object>().ToList();
	}

	private Task SendError(string message) {
		return Clients.Caller.SendAsync("ReceiveError", new { message });
	}

	private Task BroadcastReservationsChanged(string action, string message, Reservation? previousReservation, Reservation? reservation) {
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
}
