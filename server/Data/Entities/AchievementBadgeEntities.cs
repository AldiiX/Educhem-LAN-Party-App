using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace server.Data.Entities;

[Table("Achievements", Schema = "public")]
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

    public ICollection<BadgeAchievement>? BadgeRequirements { get; set; }
    public ICollection<AccountAchievement>? AccountAchievements { get; set; }
}


[Table("Badges", Schema = "public")]
public sealed class Badge : AuditableEntity<Guid> {
    [MaxLength(128)]
    public required string Name { get; set; }

    [MaxLength(512)]
    public string? Description { get; set; }

    [MaxLength(512)]
    public string? IconUrl { get; set; }

    public ICollection<BadgeAchievement>? BadgeAchievements { get; set; }
    public ICollection<AccountBadge>? AccountBadges { get; set; }
}


[Table("BadgeAchievements", Schema = "public")]
public sealed class BadgeAchievement : AuditableEntity<Guid> {
    public required Badge Badge { get; set; }
    public required Achievement Achievement { get; set; }
}


[Table("AccountAchievements", Schema = "public")]
public sealed class AccountAchievement : AuditableEntity<Guid> {
    public Guid AccountId { get; set; }
    public required Account Account { get; set; }
    public Guid AchievementId { get; set; }
    public required Achievement Achievement { get; set; }
    public bool IsHidden { get; set; } = false;
}


[Table("AccountBadges", Schema = "public")]
public sealed class AccountBadge : AuditableEntity<Guid> {
    public Guid AccountId { get; set; }
    public required Account Account { get; set; }
    public Guid BadgeId { get; set; }
    public required Badge Badge { get; set; }
    public bool IsTakenOut { get; set; } = false;
}
