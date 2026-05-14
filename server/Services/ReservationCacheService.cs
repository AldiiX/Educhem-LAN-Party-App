using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using server.Data;
using server.Data.Entities;
using server.Dto;
using server.Dto.Mappers;
using server.Infrastructure;

namespace server.Services;

public sealed class ReservationCacheService(
	AppDbContext db,
	IMemoryCache cache
) {
	private const string AuthenticatedReservationsKey = "reservations:authenticated";
	private const string AnonymousReservationsKey = "reservations:anonymous";
	private const string RoomsAndComputersKey = "reservations:rooms-and-computers";
	private const string StatusKey = "reservations:status";

	// zamky, at se pri prazdny cache nerozjede deset stejnejch db dotazu naraz
	private static readonly SemaphoreSlim ReservationsLock = new(1, 1);
	private static readonly SemaphoreSlim RoomsAndComputersLock = new(1, 1);
	private static readonly SemaphoreSlim StatusLock = new(1, 1);

	public async Task<List<object>> GetReservationsAsync(bool authenticated) {
		var cacheKey = authenticated ? AuthenticatedReservationsKey : AnonymousReservationsKey;
		if (cache.TryGetValue(cacheKey, out List<object>? reservations) && reservations != null) {
			return reservations;
		}

		await ReservationsLock.WaitAsync();
		try {
			// mezitim co jsme cekali, ji mohl naplnit jinej request
			if (cache.TryGetValue(cacheKey, out reservations) && reservations != null) {
				return reservations;
			}

			var snapshot = await LoadReservationSnapshotAsync();
			SetReservationSnapshot(snapshot);

			return authenticated ? snapshot.Authenticated : snapshot.Anonymous;
		} finally {
			ReservationsLock.Release();
		}
	}

	public async Task RefreshReservationsAsync() {
		await ReservationsLock.WaitAsync();
		try {
			SetReservationSnapshot(await LoadReservationSnapshotAsync());
		} finally {
			ReservationsLock.Release();
		}
	}

	public async Task ApplyReservationChangeAsync(Reservation? previousReservation, Reservation? reservation) {
		await ReservationsLock.WaitAsync();
		try {
			// po book/unbook nechcem tahat vse znova z db, jen prepisem cache deltou
			if (cache.TryGetValue(AuthenticatedReservationsKey, out List<object>? authenticatedReservations) && authenticatedReservations != null) {
				cache.Set(AuthenticatedReservationsKey, ApplyReservationChange(
					authenticatedReservations,
					previousReservation?.Id,
					reservation?.ToDto()
				));
			}

			if (cache.TryGetValue(AnonymousReservationsKey, out List<object>? anonymousReservations) && anonymousReservations != null) {
				cache.Set(AnonymousReservationsKey, ApplyReservationChange(
					anonymousReservations,
					previousReservation?.Id,
					reservation?.ToAnonymousDto()
				));
			}

			InvalidateStatus();
		} finally {
			ReservationsLock.Release();
		}
	}

	public void InvalidateReservations() {
		cache.Remove(AuthenticatedReservationsKey);
		cache.Remove(AnonymousReservationsKey);
		InvalidateStatus();
	}

	public async Task<RoomsAndComputersCacheDto> GetRoomsAndComputersAsync() {
		if (cache.TryGetValue(RoomsAndComputersKey, out RoomsAndComputersCacheDto? roomsAndComputers) && roomsAndComputers != null) {
			return roomsAndComputers;
		}

		await RoomsAndComputersLock.WaitAsync();
		try {
			// stejnej doublecheck jak u rezervaci, klasickej cache stampede fixik
			if (cache.TryGetValue(RoomsAndComputersKey, out roomsAndComputers) && roomsAndComputers != null) {
				return roomsAndComputers;
			}

			var computers = await db.ComputersEf().AsNoTracking().ToListAsync();
			var rooms = await db.RoomsEf().AsNoTracking().ToListAsync();

			roomsAndComputers = new RoomsAndComputersCacheDto(
				computers.Select(c => c.ToDto()).ToList(),
				rooms.Select(r => r.ToDto()).ToList()
			);

			cache.Set(RoomsAndComputersKey, roomsAndComputers);
			return roomsAndComputers;
		} finally {
			RoomsAndComputersLock.Release();
		}
	}

	public void InvalidateRoomsAndComputers() {
		cache.Remove(RoomsAndComputersKey);
		InvalidateStatus();
	}

	public async Task<ReservationStatusCacheDto> GetStatusAsync() {
		if (cache.TryGetValue(StatusKey, out ReservationStatusCacheDto? status) && status != null) {
			return status;
		}

		await StatusLock.WaitAsync();
		try {
			// status je jen rychlej prehled, tak mu staci kratka cache a nemusime porad pocitat db
			if (cache.TryGetValue(StatusKey, out status) && status != null) {
				return status;
			}

			var roomsAndComputers = await GetRoomsAndComputersAsync();
			var reservations = await db.Reservations.AsNoTracking().CountAsync();
			var accounts = await db.Accounts.AsNoTracking().ToListAsync();
			var maxCapacity = roomsAndComputers.Computers.Count + roomsAndComputers.Rooms.Sum(r => r.Capacity);
			var accountsWithEnabledReservations = accounts.Count(a => a.EnableReservations);

			status = new ReservationStatusCacheDto(
				maxCapacity,
				accountsWithEnabledReservations,
				accounts.Count == 0 ? 0 : Math.Min(Math.Round((double)accountsWithEnabledReservations / accounts.Count * 100), 100),
				reservations,
				maxCapacity == 0 ? 0 : Math.Min(Math.Round((double)reservations / maxCapacity * 100), 100)
			);

			cache.Set(StatusKey, status, TimeSpan.FromSeconds(30));
			return status;
		} finally {
			StatusLock.Release();
		}
	}

	public void InvalidateStatus() {
		cache.Remove(StatusKey);
	}

	private async Task<ReservationCacheSnapshot> LoadReservationSnapshotAsync() {
		var reservations = await db.ReservationsEf().AsNoTracking().ToListAsync();

		return new ReservationCacheSnapshot(
			reservations.Select(x => x.ToDto()).Cast<object>().ToList(),
			reservations.Select(x => x.ToAnonymousDto()).Cast<object>().ToList()
		);
	}

	private void SetReservationSnapshot(ReservationCacheSnapshot snapshot) {
		cache.Set(AuthenticatedReservationsKey, snapshot.Authenticated);
		cache.Set(AnonymousReservationsKey, snapshot.Anonymous);
	}

	private static List<object> ApplyReservationChange(List<object> reservations, Guid? previousReservationId, object? reservation) {
		// prvne zahodime starou rezervaci, pak pripadne pridame novou nahoru
		var nextReservations = previousReservationId == null
			? reservations
			: RemoveReservation(reservations, previousReservationId.Value);

		if (reservation is not AuditableEntityDto<Guid> nextReservation) return nextReservations;

		return [
			reservation,
			..RemoveReservation(nextReservations, nextReservation.Id)
		];
	}

	private static List<object> RemoveReservation(List<object> reservations, Guid reservationId) {
		return reservations
			.Where(r => r is not AuditableEntityDto<Guid> reservation || reservation.Id != reservationId)
			.ToList();
	}

	private sealed record ReservationCacheSnapshot(
		List<object> Authenticated,
		List<object> Anonymous
	);
}

public sealed record RoomsAndComputersCacheDto(
	List<ComputerDto> Computers,
	List<RoomDto> Rooms
);

public sealed record ReservationStatusCacheDto(
	int MaxCapacity,
	int AccountsWithEnabledReservations,
	double AccountsWithEnabledReservationsPercentage,
	int CapacityUsed,
	double CapacityUsedPercentage
);
