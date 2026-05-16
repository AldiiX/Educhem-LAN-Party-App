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

	extension(AccountAchievement accountAchievement) {
		public AccountAchievementDto ToDto() {
			return new AccountAchievementDto {
				Id = accountAchievement.Id,
				Achievement = accountAchievement.Achievement.ToDto(),
				IsHidden = accountAchievement.IsHidden,
				CreatedAtUtc = accountAchievement.CreatedAtUtc,
			};
		}
	}

	extension(AccountBadge accountBadge) {
		public AccountBadgeDto ToDto() {
			return new AccountBadgeDto {
				Id = accountBadge.Id,
				Badge = accountBadge.Badge.ToDto(),
				IsTakenOut = accountBadge.IsTakenOut,
			};
		}
	}
}
