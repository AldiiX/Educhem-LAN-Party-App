using Microsoft.EntityFrameworkCore;

namespace server.Data.Entities;

public sealed  class ComputerReservation : Reservation {
	[DeleteBehavior(DeleteBehavior.Cascade)]
	public required Computer Computer { get; set; }
}