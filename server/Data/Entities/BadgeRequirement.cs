using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using server.Data.Attributes;

namespace server.Data.Entities;

[Table("BadgeRequirements", Schema = "achievements")]
[PrimaryKey(nameof(BadgeId), nameof(AchievementId))]
public sealed class BadgeRequirement {
    public Guid BadgeId { get; set; }

    [ForeignKey(nameof(BadgeId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public required Badge Badge { get; set; }

    public Guid AchievementId { get; set; }

    [AutoInclude]
    [ForeignKey(nameof(AchievementId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public required Achievement Achievement { get; set; }
}