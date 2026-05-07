using server.Data.Entities;

namespace server.Dto.Mappers;

public static class AccountMapper {
	extension(Account account) {
		public AccountBaseDto ToBaseDto() {
			return new AccountBaseDto {
				Id = account.Id,
				FirstName = account.FirstName,
				LastName = account.LastName,
				Class = account.Class,
				School = account.School,
				AvatarUrl = account.AvatarUrl,
				BannerUrl = account.BannerUrl,
			};
		}

		public AccountDto ToDto() {
			return new AccountDto {
				Id = account.Id,
				FirstName = account.FirstName,
				LastName = account.LastName,
				Class = account.Class,
				School = account.School,
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

		public AccountSessionDto ToSessionDto() {
			return new AccountSessionDto() {
				Id = account.Id,
				FirstName = account.FirstName,
				LastName = account.LastName,
				Class = account.Class,
				School = account.School,
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