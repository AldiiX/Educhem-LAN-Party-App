using server.Data.Entities;

namespace server.Dto.Mappers;

public static class AccountMapper {
	extension(Account account) {
		public ProfileDto ToProfileDto(bool deep = true) {
			var achievements = account.AccountAchievements
				?.Where(x => !x.IsHidden && !x.Achievement.IsHidden)
				.Select(x => x.ToDto())
				.ToList() ?? new List<AccountAchievementDto>();
			var badges = account.AccountBadges
				?.Where(x => x.IsTakenOut)
				.Select(x => x.ToDto())
				.ToList() ?? new List<AccountBadgeDto>();

			return new ProfileDto {
				Id = account.Id,
				FirstName = account.FirstName,
				LastName = account.LastName,
				Enrollment = account.Enrollment?.ToDto(),
				AvatarUrl = account.AvatarUrl,
				BannerUrl = account.BannerUrl,
				DiscordUsername = account.OAuthConnections.FirstOrDefault(item => item.Provider == OAuthProvider.Discord)?.Username,
				GitHubUsername = account.OAuthConnections.FirstOrDefault(item => item.Provider == OAuthProvider.GitHub)?.Username,
				GitHubProfileUrl = account.OAuthConnections.FirstOrDefault(item => item.Provider == OAuthProvider.GitHub)?.ProfileUrl,
				GoogleName = account.OAuthConnections.FirstOrDefault(item => item.Provider == OAuthProvider.Google)?.Username,
				SteamUsername = account.OAuthConnections.FirstOrDefault(item => item.Provider == OAuthProvider.Steam)?.Username,
				SteamProfileUrl = account.OAuthConnections.FirstOrDefault(item => item.Provider == OAuthProvider.Steam)?.ProfileUrl,
				Gender = account.Gender,
				CreatedAtUtc = account.CreatedAtUtc,
				AccountType = account.AccountType,
				Achievements = achievements,
				Badges = badges,
			};
		}

		public AccountDto ToDto(bool deep = true) {
			var achievements = account.AccountAchievements
				?.Select(x => x.ToDto())
				.ToList() ?? new List<AccountAchievementDto>();
			var badges = account.AccountBadges
				?.Select(x => x.ToDto())
				.ToList() ?? new List<AccountBadgeDto>();

			return new AccountDto {
				Id = account.Id,
				FirstName = account.FirstName,
				LastName = account.LastName,
				Enrollment = account.Enrollment?.ToDto(),
				AvatarUrl = account.AvatarUrl,
				BannerUrl = account.BannerUrl,
				DiscordUsername = account.OAuthConnections.FirstOrDefault(item => item.Provider == OAuthProvider.Discord)?.Username,
				GitHubUsername = account.OAuthConnections.FirstOrDefault(item => item.Provider == OAuthProvider.GitHub)?.Username,
				GitHubProfileUrl = account.OAuthConnections.FirstOrDefault(item => item.Provider == OAuthProvider.GitHub)?.ProfileUrl,
				GoogleName = account.OAuthConnections.FirstOrDefault(item => item.Provider == OAuthProvider.Google)?.Username,
				SteamUsername = account.OAuthConnections.FirstOrDefault(item => item.Provider == OAuthProvider.Steam)?.Username,
				SteamProfileUrl = account.OAuthConnections.FirstOrDefault(item => item.Provider == OAuthProvider.Steam)?.ProfileUrl,
				UpdatedAtUtc = account.UpdatedAtUtc,
				CreatedAtUtc = account.CreatedAtUtc,
				LastActiveUtc = account.LastActiveUtc,
				Email = account.Email,
				Gender = account.Gender,
				EnableReservations = account.EnableReservations,
				AccountType = account.AccountType,
				Achievements = achievements,
				Badges = badges,
				CommunicationStyle = account.CommunicationStyle,
				AvatarSyncPlatform = account.AvatarSyncPlatform,
			};
		}

		public AccountSessionDto ToSessionDto(bool deep = true) {
			var achievements = account.AccountAchievements
				?.Select(x => x.ToDto())
				.ToList() ?? new List<AccountAchievementDto>();
			var badges = account.AccountBadges
				?.Select(x => x.ToDto())
				.ToList() ?? new List<AccountBadgeDto>();

			return new AccountSessionDto() {
				Id = account.Id,
				FirstName = account.FirstName,
				LastName = account.LastName,
				Enrollment = account.Enrollment?.ToDto(),
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
				CommunicationStyle = account.CommunicationStyle,
			};
		}
	}
}
