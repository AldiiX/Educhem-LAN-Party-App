using Microsoft.EntityFrameworkCore;
using server.Data.Entities;

namespace server.Data;

public class AppDbContext : DbContext {



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
				.HasDefaultValue("uuidv7()");

			e.Property(a => a.LastActiveUtc)
				.HasColumnType("timestamp with time zone")
				.ValueGeneratedOnAdd()
				.HasDefaultValueSql("now()");

			e.Property(a => a.EnableReservations)
				.ValueGeneratedOnAdd()
				.HasDefaultValue(false);
		});
	}
}