using server.Data.Entities;

namespace server.Dto.Mappers;

public static class AccountMapper {
	extension(Account account) {
		public ProfileDto ToProfileDto(bool deep = true) {
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
			};
		}

		public AccountDto ToDto(bool deep = true) {
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
			};
		}

		public AccountSessionDto ToSessionDto(bool deep = true) {
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
				PasswordHash = account.PasswordHash,
			};
		}
	}
}