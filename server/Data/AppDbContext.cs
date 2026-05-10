using Microsoft.EntityFrameworkCore;
using server.Data.Entities;

namespace server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options) {

	public DbSet<Account> Accounts { get; set; }
			public DbSet<Achievement> Achievements { get; set; }
			public DbSet<Badge> Badges { get; set; }
			public DbSet<BadgeAchievement> BadgeAchievements { get; set; }
			public DbSet<AccountAchievement> AccountAchievements { get; set; }
			public DbSet<AccountBadge> AccountBadges { get; set; }



	public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
		var nowUtc = DateTime.UtcNow;

		foreach (var entry in ChangeTracker.Entries<IAuditable>()) {
			if (entry.State == EntityState.Added) {
				entry.Entity.CreatedAtUtc = nowUtc;
				entry.Entity.UpdatedAtUtc = nowUtc;
			} else if (entry.State == EntityState.Modified) {
				entry.Entity.UpdatedAtUtc = nowUtc;

				entry.Property(nameof(IAuditable.CreatedAtUtc)).IsModified = false;
			}
		}

		return await base.SaveChangesAsync(cancellationToken);
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		foreach (var entityType in modelBuilder.Model.GetEntityTypes()) {
			var clrType = entityType.ClrType;
			if (!typeof(IAuditable).IsAssignableFrom(clrType)) continue;

			var builder = modelBuilder.Entity(clrType);

			builder.Property(nameof(IAuditable.CreatedAtUtc))
				.HasColumnType("timestamp with time zone")
				.ValueGeneratedOnAdd()
				.HasDefaultValueSql("now()");

			builder.Property(nameof(IAuditable.UpdatedAtUtc))
				.HasColumnType("timestamp with time zone")
				.ValueGeneratedOnAdd()
				.HasDefaultValueSql("now()");
		}

		modelBuilder.Entity<Account>(e => {
			e.Property(a => a.Id)
				.ValueGeneratedOnAdd()
				.HasDefaultValueSql("uuidv7()");

			e.Property(a => a.LastActiveUtc)
				.HasColumnType("timestamp with time zone")
				.ValueGeneratedOnAdd()
				.HasDefaultValueSql("now()");

			e.Property(a => a.EnableReservations)
				.ValueGeneratedOnAdd()
				.HasDefaultValue(false);

			e.Property(a => a.AccountType)
				.ValueGeneratedOnAdd()
				.HasDefaultValue(AccountType.Student);
		});

		modelBuilder.Entity<Achievement>(e => {
			e.Property(a => a.Id)
				.ValueGeneratedOnAdd()
				.HasDefaultValueSql("uuidv7()");
				e.Property(a => a.IsHidden)
					.ValueGeneratedOnAdd()
					.HasDefaultValue(false);
		});

		modelBuilder.Entity<Badge>(e => {
			e.Property(a => a.Id)
				.ValueGeneratedOnAdd()
				.HasDefaultValueSql("uuidv7()");
		});

		modelBuilder.Entity<BadgeAchievement>(e => {
			e.HasIndex("BadgeId", "AchievementId").IsUnique();
			e.HasOne(b => b.Badge).WithMany(x => x.BadgeAchievements).OnDelete(DeleteBehavior.Cascade);
			e.HasOne(b => b.Achievement).WithMany(x => x.BadgeRequirements).OnDelete(DeleteBehavior.Cascade);
			e.Property(x => x.Id).ValueGeneratedOnAdd().HasDefaultValueSql("uuidv7()");
		});

		modelBuilder.Entity<AccountAchievement>(e => {
			e.HasIndex("AccountId", "AchievementId").IsUnique();
			e.HasOne(x => x.Account).WithMany().OnDelete(DeleteBehavior.Cascade);
			e.HasOne(x => x.Achievement).WithMany(a => a.AccountAchievements).OnDelete(DeleteBehavior.Cascade);
			e.Property(x => x.Id).ValueGeneratedOnAdd().HasDefaultValueSql("uuidv7()");
		});

		modelBuilder.Entity<AccountBadge>(e => {
			e.HasIndex("AccountId", "BadgeId").IsUnique();
			e.HasOne(x => x.Account).WithMany().OnDelete(DeleteBehavior.Cascade);
			e.HasOne(x => x.Badge).WithMany(b => b.AccountBadges).OnDelete(DeleteBehavior.Cascade);
			e.Property(x => x.Id).ValueGeneratedOnAdd().HasDefaultValueSql("uuidv7()");
		});
	}
}