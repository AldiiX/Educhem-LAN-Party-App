using server.Data.Entities;

namespace server.Dto;

public sealed record PaginationDto(
	int Page,
	int PageSize,
	int TotalEntries,
	int TotalPages
);

public sealed record ValueCountDto<TValue>(
	TValue Value,
	int Count
);

public sealed record AccountSchoolFilterOptionDto(
	SchoolDto School,
	int Count
);

public sealed record AccountFilterOptionsDto(
	IReadOnlyList<ValueCountDto<AccountType>> AccountTypes,
	IReadOnlyList<ValueCountDto<Gender>> Genders,
	IReadOnlyList<ValueCountDto<string>> Classes,
	IReadOnlyList<AccountSchoolFilterOptionDto> Schools
);

public sealed record AdministrationAccountsPageDto(
	IReadOnlyList<AccountDto> Accounts,
	PaginationDto Pagination,
	int TotalItems,
	AccountFilterOptionsDto FilterOptions
);

public sealed record LogFilterOptionsDto(
	IReadOnlyList<ValueCountDto<LogType>> LogTypes,
	IReadOnlyList<ValueCountDto<string>> ExactTypes
);

public sealed record AdministrationLogsPageDto(
	IReadOnlyList<LogEntryDto> Logs,
	PaginationDto Pagination,
	int TotalItems,
	LogFilterOptionsDto FilterOptions
);
