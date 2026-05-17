using Microsoft.EntityFrameworkCore;

using server.Data.Attributes;

namespace server.Data.Entities;

public sealed class RoomReservation : Reservation {
	[AutoInclude]
	[DeleteBehavior(DeleteBehavior.Cascade)]
	public required Room Room { get; set; }
}
