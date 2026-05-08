using server.Data.Entities;

namespace server.Dto.Mappers;

public static class Mapper {
	public static SchoolDto ToDto(this School school) {
		return new SchoolDto {
			Id = school.Id,
			Slug = school.Slug,
			DisplayName = school.DisplayName,
			IconUrl = school.IconUrl,
		};
	}
}