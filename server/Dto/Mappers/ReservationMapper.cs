using server.Data.Entities;

namespace server.Dto.Mappers;

public static class ReservationMapper {
	public static ComputerDto ToDto(this Computer computer, bool deep = true) {
		return new ComputerDto() {
			Id = computer.Id,
			ImageUrl = computer.ImageUrl ?? computer.Room?.ImageUrl,
			Room = computer.Room?.ToDto(false),
			Available = computer.Available,
			IsTeachersComputer = computer.IsTeachersComputer,
			Label =  computer.Label ?? computer.Id,
		};
	}

	public static RoomDto ToDto(this Room room, bool deep = true) {
		return new RoomDto() {
			Id = room.Id,
			Label = room.Label ?? room.Id,
			Capacity = room.Capacity,
			Available = room.Available,
			ImageUrl = room.ImageUrl,
		};
	}



	public static ReservationDto ToDto(this Reservation reservation, bool deep = true) {
		return new ReservationDto() {
			Id = reservation.Id,
			Profile = reservation.Account.ToProfileDto(),
			Note = reservation.Note,
			UpdatedAtUtc = reservation.UpdatedAtUtc,
			CreatedAtUtc = reservation.CreatedAtUtc,
			Computer = reservation is ComputerReservation cr ? cr.Computer.ToDto(false) : null,
			Room = reservation is RoomReservation rr ? rr.Room.ToDto(false) : null,
		};
	}

	public static AnonymousReservationDto ToAnonymousDto(this Reservation reservation, bool deep = true) {
		return new AnonymousReservationDto() {
			Id = reservation.Id,
			Note = reservation.Note,
			UpdatedAtUtc = reservation.UpdatedAtUtc,
			CreatedAtUtc = reservation.CreatedAtUtc,
			Computer = reservation is ComputerReservation cr ? cr.Computer.ToDto(false) : null,
			Room = reservation is RoomReservation rr ? rr.Room.ToDto(false) : null,
		};
	}
}
