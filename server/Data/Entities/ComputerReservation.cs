namespace server.Data.Entities;

public sealed  class ComputerReservation : Reservation {
	public required Computer Computer { get; set; }
}