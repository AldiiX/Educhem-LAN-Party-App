namespace server.Infrastructure;

internal static class EnumFilters {
	public static TEnum[] ParseEnumFilters<TEnum>(IEnumerable<string>? values) where TEnum : struct, Enum {
		return values?
			.Select(value => Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : (TEnum?)null)
			.Where(value => value.HasValue)
			.Select(value => value!.Value)
			.Distinct()
			.ToArray() ?? [];
	}
}
