using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using server.Data.Attributes;

namespace server.Data.Entities;

[Table("Enrollments", Schema = "public")]
public sealed class Enrollment {
	[Key]
	public Guid AccountId { get; set; }
	public Account Account { get; set; } = null!;

	public ushort SchoolId { get; set; }
	[AutoInclude]
	public School School { get; set; } = null!;

	[MaxLength(16)]
	public string? Class { get; set; }
}
