using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using server.Data.Attributes;

namespace server.Data.Entities;

[Table("Enrollments", Schema = "public")]
public sealed class Enrollment {
	[Key]
	public Guid AccountId { get; set; }
	public required Account Account { get; set; }

	public ushort SchoolId { get; set; }
	[AutoInclude]
	public required School School { get; set; }

	[MaxLength(16)]
	public string? Class { get; set; }
}
