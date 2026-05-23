using server.Data.Entities;

namespace server.Dto.Mappers;

public static class AttendanceMapper {
	extension(AttendanceEntry entry) {
		public AttendanceEntryDto ToDto(bool deep = true) {
			return new AttendanceEntryDto {
				Id = entry.Id,
				Type = entry.Type,
				Reason = entry.Reason,
				Profile = entry.Account.ToProfileDto(),
				CreatedBy = entry.CreatedBy.ToProfileDto(),
				CreatedAtUtc = entry.CreatedAtUtc,
				UpdatedAtUtc = entry.UpdatedAtUtc,
			};
		}
	}
}
