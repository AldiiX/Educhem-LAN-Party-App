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
}