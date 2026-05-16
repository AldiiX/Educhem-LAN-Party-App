using Microsoft.EntityFrameworkCore;

namespace server.Data.Entities;

public sealed class RoomReservation : Reservation {
	[DeleteBehavior(DeleteBehavior.Cascade)]
	public required Room Room { get; set; }
}