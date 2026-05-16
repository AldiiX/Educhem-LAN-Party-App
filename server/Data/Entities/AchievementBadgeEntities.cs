using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace server.Data.Entities;

[Table("Achievements", Schema = "achievements")]
public sealed class Achievement : AuditableEntity<Guid> {
    [MaxLength(128)]
    public required string Key { get; set; }

    [MaxLength(128)]
    public required string Name { get; set; }

    [MaxLength(512)]
    public string? Description { get; set; }

    [MaxLength(512)]
    public string? IconUrl { get; set; }

    public bool IsHidden { get; set; } = false;

    public ICollection<BadgeRequirement>? BadgeRequirements { get; set; }
    public ICollection<AccountAchievement>? AccountAchievements { get; set; }
}


[Table("Badges", Schema = "achievements")]
public sealed class Badge : AuditableEntity<Guid> {
    [MaxLength(128)]
    public required string Name { get; set; }

    [MaxLength(512)]
    public string? Description { get; set; }

    [MaxLength(512)]
    public string? IconUrl { get; set; }

    public ICollection<BadgeRequirement>? Requirements { get; set; }
    public ICollection<AccountBadge>? AccountBadges { get; set; }
}


[Table("BadgeRequirements", Schema = "achievements")]
[PrimaryKey(nameof(BadgeId), nameof(AchievementId))]
public sealed class BadgeRequirement {
    public Guid BadgeId { get; set; }

    [ForeignKey(nameof(BadgeId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public required Badge Badge { get; set; }

    public Guid AchievementId { get; set; }

    [ForeignKey(nameof(AchievementId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public required Achievement Achievement { get; set; }
}


[Table("AccountAchievements", Schema = "achievements")]
[Index(nameof(AccountId), nameof(AchievementId), IsUnique = true)]
public sealed class AccountAchievement : AuditableEntity<Guid> {
    public Guid AccountId { get; set; }

    [ForeignKey(nameof(AccountId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public required Account Account { get; set; }

    public Guid AchievementId { get; set; }

    [ForeignKey(nameof(AchievementId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public required Achievement Achievement { get; set; }

    public bool IsHidden { get; set; } = false;
}


[Table("AccountBadges", Schema = "achievements")]
[Index(nameof(AccountId), nameof(BadgeId), IsUnique = true)]
public sealed class AccountBadge : AuditableEntity<Guid> {
    public Guid AccountId { get; set; }

    [ForeignKey(nameof(AccountId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public required Account Account { get; set; }

    public Guid BadgeId { get; set; }

    [ForeignKey(nameof(BadgeId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public required Badge Badge { get; set; }

    public bool IsTakenOut { get; set; } = false;
}
