using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Entities;

namespace server.Infrastructure;

public static class EntityFrameworkIncludes {

	public static IQueryable<Account> AccountsEf(this AppDbContext db) {
		return db.Accounts
			.Include(a => a.AccountAchievements)
				.ThenInclude(x => x.Achievement)
			.Include(a => a.AccountBadges)
				.ThenInclude(x => x.Badge)
			.AsSplitQuery();
	}

	public static IQueryable<AccountAchievement> AccountAchievementsEf(this AppDbContext db) {
		return db.AccountAchievements;
	}

	public static IQueryable<AccountBadge> AccountBadgesEf(this AppDbContext db) {
		return db.AccountBadges;
	}

	public static IQueryable<Reservation> ReservationsEf(this AppDbContext db) {
		return db.Reservations
			.Include(r => r.Account)
				.ThenInclude(a => a.AccountAchievements)
					.ThenInclude(x => x.Achievement)
			.Include(r => r.Account)
				.ThenInclude(a => a.AccountBadges)
					.ThenInclude(x => x.Badge)
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
			.AsSplitQuery();
	}

	public static IQueryable<ProblemReport> ProblemReportsEf(this AppDbContext db) {
		return db.ProblemReports

			// filtered reporter collections
			.Include(pr => pr.Reporter)
				.ThenInclude(a => a.AccountAchievements)
					.ThenInclude(aa => aa.Achievement)
			.Include(pr => pr.Reporter)
				.ThenInclude(a => a.AccountBadges)
					.ThenInclude(ab => ab.Badge)

			.Include(pr => pr.ResolvedBy)
				.ThenInclude(a => a!.AccountAchievements)
					.ThenInclude(aa => aa.Achievement)
			.Include(pr => pr.ResolvedBy)
				.ThenInclude(a => a!.AccountBadges)
					.ThenInclude(ab => ab.Badge)

			.OrderByDescending(pr => pr.CreatedAtUtc)
			.AsSplitQuery();
	}

	public static IQueryable<AttendanceEntry> AttendanceEntriesEf(this AppDbContext db) {
		return db.AttendanceEntries
			.Include(ae => ae.Account)
				.ThenInclude(a => a.AccountAchievements)
					.ThenInclude(aa => aa.Achievement)
			.Include(ae => ae.Account)
				.ThenInclude(a => a.AccountBadges)
					.ThenInclude(ab => ab.Badge)
			.Include(ae => ae.CreatedBy)
				.ThenInclude(a => a.AccountAchievements)
					.ThenInclude(aa => aa.Achievement)
			.Include(ae => ae.CreatedBy)
				.ThenInclude(a => a.AccountBadges)
					.ThenInclude(ab => ab.Badge)
			.OrderByDescending(ae => ae.CreatedAtUtc)
			.AsSplitQuery();
	}
}
