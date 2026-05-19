using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using server.Data.Attributes;

namespace server.Data.Entities;


[Table("Achievements", Schema = "achievements")]
[UuidV7]
public sealed class Achievement : AuditableEntity<Guid> {
    [MaxLength(128)]
    public required string Key { get; set; }

    [MaxLength(128)]
    public required string Name { get; set; }

    [MaxLength(512)]
    public string? Description { get; set; }

    [MaxLength(512)]
    public string? IconUrl { get; set; }

    [DefaultValue(false)]
    public bool IsHidden { get; set; } = false;

    public ICollection<BadgeRequirement>? BadgeRequirements { get; set; }
    public ICollection<AccountAchievement>? AccountAchievements { get; set; }
}
