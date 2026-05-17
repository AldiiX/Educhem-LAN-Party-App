using server.Data.Entities;

namespace server.Dto.Mappers;

public static class Mapper {
	public static SchoolDto ToDto(this School school, bool deep = true) {
		return new SchoolDto {
			Id = school.Id,
			Slug = school.Slug,
			ShortName = school.ShortName,
			DisplayName = school.DisplayName,
			IconUrl = school.IconUrl,
		};
	}
}
