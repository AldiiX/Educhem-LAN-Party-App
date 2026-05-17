using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using server.Data.Attributes;
using server.Data.Entities;

namespace server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options) {

	public DbSet<Account> Accounts { get; set; }

	public DbSet<Achievement> Achievements { get; set; }
	public DbSet<Badge> Badges { get; set; }
	public DbSet<AccountAchievement> AccountAchievements { get; set; }
	public DbSet<AccountBadge> AccountBadges { get; set; }

	public DbSet<Computer> Computers { get; set; }
	public DbSet<Room> Rooms { get; set; }

	public DbSet<Reservation> Reservations { get; set; }
	public DbSet<ComputerReservation> ComputerReservations { get; set; }
	public DbSet<RoomReservation> RoomReservations { get; set; }

	public DbSet<ProblemReport> ProblemReports { get; set; }



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
			var entityBuilder = modelBuilder.Entity(clrType);

			foreach (var property in entityType.GetProperties()) {
				var propertyInfo = property.PropertyInfo;
				if (propertyInfo == null) continue;

				var propertyBuilder = entityBuilder.Property(property.Name);
				var hasEntityUuidV7 = property.Name == nameof(Entity<Guid>.Id)
					&& clrType.IsDefined(typeof(UuidV7Attribute), inherit: true);
				var defaultValueSql = propertyInfo.GetCustomAttributes(typeof(DefaultValueSqlAttribute), inherit: true)
					.OfType<DefaultValueSqlAttribute>()
					.FirstOrDefault();
				var defaultValue = propertyInfo.GetCustomAttributes(typeof(DefaultValueAttribute), inherit: true)
					.OfType<DefaultValueAttribute>()
					.FirstOrDefault();

				if (hasEntityUuidV7 && property.ClrType == typeof(Guid)) {
					propertyBuilder.HasDefaultValueSql("uuidv7()");
				} else if (defaultValueSql != null && (defaultValueSql is not UuidV7Attribute || property.ClrType == typeof(Guid))) {
					propertyBuilder.HasDefaultValueSql(defaultValueSql.Sql);
				}

				if (defaultValue != null) {
					propertyBuilder.HasDefaultValue(defaultValue.Value);
				}

				if (propertyInfo.IsDefined(typeof(StringEnumAttribute), inherit: true)) {
					propertyBuilder.HasConversion<string>();
				}
			}

			foreach (var navigation in entityType.GetNavigations()) {
				var hasAutoInclude = navigation.PropertyInfo?.IsDefined(typeof(AutoIncludeAttribute), inherit: true) == true;

				if (hasAutoInclude) {
					entityBuilder.Navigation(navigation.Name).AutoInclude();
				}
			}
		}
	}
}
