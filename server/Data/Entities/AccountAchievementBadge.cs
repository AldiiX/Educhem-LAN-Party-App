using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using server.Data.Attributes;

namespace server.Data.Entities;

[Table("AccountAchievements", Schema = "achievements")]
[Index(nameof(AccountId), nameof(AchievementId), IsUnique = true)]
[UuidV7]
public sealed class AccountAchievement : AuditableEntity<Guid> {
    public Guid AccountId { get; set; }

    [ForeignKey(nameof(AccountId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public required Account Account { get; set; }

    public Guid AchievementId { get; set; }

    [AutoInclude]
    [ForeignKey(nameof(AchievementId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public required Achievement Achievement { get; set; }

    [DefaultValue(false)]
    public bool IsHidden { get; set; } = false;
}


[Table("AccountBadges", Schema = "achievements")]
[Index(nameof(AccountId), nameof(BadgeId), IsUnique = true)]
[UuidV7]
public sealed class AccountBadge : AuditableEntity<Guid> {
    public Guid AccountId { get; set; }

    [ForeignKey(nameof(AccountId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public required Account Account { get; set; }

    public Guid BadgeId { get; set; }

    [AutoInclude]
    [ForeignKey(nameof(BadgeId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public required Badge Badge { get; set; }

    [DefaultValue(false)]
    public bool IsTakenOut { get; set; } = false;
}
