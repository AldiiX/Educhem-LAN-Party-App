using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Entities;

namespace server.Infrastructure;

public static class EntityFrameworkIncludes {

	public static IQueryable<Account> AccountsEf(this AppDbContext db) {
		return db.Accounts
			.Include(a => a.School)
			.Include(a => a.AccountAchievements)
				.ThenInclude(x => x.Achievement)
			.Include(a => a.AccountBadges)
				.ThenInclude(x => x.Badge)
			.AsSplitQuery();
	}

	public static IQueryable<Reservation> ReservationsEf(this AppDbContext db) {
		return db.Reservations
			.Include(r => r.Account)
			.Include(r => ((ComputerReservation)r).Computer)
				.ThenInclude(c => c.Room)
			.Include(r => ((RoomReservation)r).Room)
			.OrderByDescending(r => r.CreatedAtUtc)
			.AsSplitQuery();
	}

	public static IQueryable<Room> RoomsEf(this AppDbContext db) {
		return db.Rooms
			.Where(r => r.Available)
			.AsSplitQuery();
	}

	public static IQueryable<Computer> ComputersEf(this AppDbContext db) {
		return db.Computers
			.Where(c => c.Available)
			.Include(c => c.Room)
			.AsSplitQuery();
	}

	public static IQueryable<ProblemReport> ProblemReportsEf(this AppDbContext db) {
		return db.ProblemReports
			.Include(r => r.Reporter)
				.ThenInclude(a => a.School)
			.Include(r => r.ResolvedBy)
				.ThenInclude(a => a!.School)
			.OrderByDescending(r => r.CreatedAtUtc)
			.AsSplitQuery();
	}
}
