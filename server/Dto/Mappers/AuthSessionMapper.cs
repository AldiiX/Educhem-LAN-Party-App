using server.Data.Entities;

namespace server.Dto.Mappers;

public static class AuthSessionMapper {
	public static AuthSessionDto ToDto(this AuthSession session, Guid? currentSessionId = null) {
		var lastActive = session.LastActiveUtc.Year < 2000 ? session.CreatedAtUtc : session.LastActiveUtc;
		return new AuthSessionDto {
			Id = session.Id,
			DeviceType = session.DeviceType,
			Browser = session.Browser,
			OperatingSystem = session.OperatingSystem,
			IpAddress = session.IpAddress,
			City = session.City,
			Country = session.Country,
			CreatedAtUtc = session.CreatedAtUtc,
			LastActiveUtc = lastActive,
			IsCurrent = currentSessionId != null && session.Id == currentSessionId.Value,
		};
	}
}
