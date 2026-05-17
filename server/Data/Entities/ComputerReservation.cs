using Microsoft.EntityFrameworkCore;

using server.Data.Attributes;

namespace server.Data.Entities;

public sealed  class ComputerReservation : Reservation {
	[AutoInclude]
	[DeleteBehavior(DeleteBehavior.Cascade)]
	public required Computer Computer { get; set; }
}
