using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using server.Data.Attributes;

namespace server.Data.Entities;



[Table("Badges", Schema = "achievements")]
[UuidV7]
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