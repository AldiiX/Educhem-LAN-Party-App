using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Data.Entities;

[Table("AppSettings", Schema="administration")]
public sealed class AppSettingsItem
{
    [Key]
    [MaxLength(128)]
    public required string Property { get; set; }

    [MaxLength(512)]
    public required string Value { get; set; }
}