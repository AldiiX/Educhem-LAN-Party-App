using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Data.Entities;

[Table("AppSettings", Schema="administration")]
public sealed class AppSetting
{
    [Key]
    [Column("property")]
    [MaxLength(128)]
    public required string Property { get; set; }

    [Column("value")]
    [MaxLength(512)]
    public required string Value { get; set; }
}