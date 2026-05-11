using server.Data.Entities;

namespace server.Dto.Mappers;

public static class AccountMapper {
	extension(Account account) {
		public ProfileDto ToProfileDto(bool deep = true) {
			var visibleAchievements = account.AccountAchievements
				?.Where(x => !x.IsHidden && !x.Achievement.IsHidden)
				.Select(x => new AccountAchievementDto {
					Id = x.Id,
					Achievement = x.Achievement.ToDto(),
					IsHidden = x.IsHidden,
					CreatedAtUtc = x.CreatedAtUtc,
				})
				.ToList() ?? new List<AccountAchievementDto>();
			var visibleBadges = account.AccountBadges
				?.Where(x => x.IsTakenOut)
				.Select(x => new AccountBadgeDto {
					Id = x.Id,
					Badge = x.Badge.ToDto(),
					IsTakenOut = x.IsTakenOut,
				})
				.ToList() ?? new List<AccountBadgeDto>();

			return new ProfileDto {
				Id = account.Id,
				FirstName = account.FirstName,
				LastName = account.LastName,
				Class = account.Class,
				School = deep ? account.School?.ToDto() : null,
				AvatarUrl = account.AvatarUrl,
				BannerUrl = account.BannerUrl,
				Gender = account.Gender,
				CreatedAtUtc = account.CreatedAtUtc,
				AccountType = account.AccountType,
				Achievements = visibleAchievements,
				Badges = visibleBadges,
			};
		}

		public AccountDto ToDto(bool deep = true) {
			var achievements = account.AccountAchievements
				?.Select(x => new AccountAchievementDto {
					Id = x.Id,
					Achievement = x.Achievement.ToDto(),
					IsHidden = x.IsHidden,
					CreatedAtUtc = x.CreatedAtUtc,
				})
				.ToList() ?? new List<AccountAchievementDto>();
			var badges = account.AccountBadges
				?.Select(x => new AccountBadgeDto {
					Id = x.Id,
					Badge = x.Badge.ToDto(),
					IsTakenOut = x.IsTakenOut,
				})
				.ToList() ?? new List<AccountBadgeDto>();

			return new AccountDto {
				Id = account.Id,
				FirstName = account.FirstName,
				LastName = account.LastName,
				Class = account.Class,
				School = deep ? account.School?.ToDto() : null,
				AvatarUrl = account.AvatarUrl,
				BannerUrl = account.BannerUrl,
				UpdatedAtUtc = account.UpdatedAtUtc,
				CreatedAtUtc = account.CreatedAtUtc,
				LastActiveUtc = account.LastActiveUtc,
				Email = account.Email,
				Gender = account.Gender,
				EnableReservations = account.EnableReservations,
				AccountType = account.AccountType,
				Achievements = achievements,
				Badges = badges,
			};
		}

		public AccountSessionDto ToSessionDto(bool deep = true) {
			var achievements = account.AccountAchievements
				?.Select(x => new AccountAchievementDto {
					Id = x.Id,
					Achievement = x.Achievement.ToDto(),
					IsHidden = x.IsHidden,
					CreatedAtUtc = x.CreatedAtUtc,
				})
				.ToList() ?? new List<AccountAchievementDto>();
			var badges = account.AccountBadges
				?.Select(x => new AccountBadgeDto {
					Id = x.Id,
					Badge = x.Badge.ToDto(),
					IsTakenOut = x.IsTakenOut,
				})
				.ToList() ?? new List<AccountBadgeDto>();

			return new AccountSessionDto() {
				Id = account.Id,
				FirstName = account.FirstName,
				LastName = account.LastName,
				Class = account.Class,
				School = deep ? account.School?.ToDto() : null,
				AvatarUrl = account.AvatarUrl,
				BannerUrl = account.BannerUrl,
				UpdatedAtUtc = account.UpdatedAtUtc,
				CreatedAtUtc = account.CreatedAtUtc,
				LastActiveUtc = account.LastActiveUtc,
				Email = account.Email,
				Gender = account.Gender,
				EnableReservations = account.EnableReservations,
				AccountType = account.AccountType,
				Achievements = achievements,
				Badges = badges,
				PasswordHash = account.PasswordHash,
			};
		}
	}
}