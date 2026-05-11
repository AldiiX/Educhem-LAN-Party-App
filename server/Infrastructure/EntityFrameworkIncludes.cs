using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Entities;

namespace server.Infrastructure;

public static class EntityFrameworkIncludes {

	public static IQueryable<Account> AccountsEf(this AppDbContext db) {
		return db.Accounts
			.Include(a => a.School)
			.AsSplitQuery();
	}

	public static IQueryable<Reservation> ReservationsEf(this AppDbContext db) {
		return db.Reservations
			.Include(r => r.Account)
			.Include(r => ((ComputerReservation)r).Computer)
			.Include(r => ((RoomReservation)r).Room)
			.AsSplitQuery();
	}
}