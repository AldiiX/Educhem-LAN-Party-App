namespace server.Data.Entities;

public sealed class RoomReservation : Reservation {
	public required Room Room { get; set; }
}