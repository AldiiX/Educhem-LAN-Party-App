using server.Data.Entities;

namespace server.Dto.Mappers;

public static class AchievementBadgeMapper {
	extension(Achievement achievement) {
		public AchievementDto ToDto() {
			return new AchievementDto {
				Id = achievement.Id,
				Key = achievement.Key,
				Name = achievement.Name,
				Description = achievement.Description,
				IconUrl = achievement.IconUrl,
				IsHidden = achievement.IsHidden,
			};
		}
	}

	extension(Badge badge) {
		public BadgeDto ToDto() {
			return new BadgeDto {
				Id = badge.Id,
				Name = badge.Name,
				Description = badge.Description,
				IconUrl = badge.IconUrl,
			};
		}
	}
}

