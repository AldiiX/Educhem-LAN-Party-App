using System.Globalization;
using System.Text.Json;
using server.Data.Entities;

namespace server.Services;

public static class CzechVocativeService {
	private static readonly CultureInfo CzechCulture = CultureInfo.GetCultureInfo("cs-CZ");
	private static readonly Lazy<IReadOnlyDictionary<string, string>> ManSuffixes = new(() => LoadSuffixes("man_suffixes.json"));
	private static readonly Lazy<IReadOnlyDictionary<string, string>> ManVsWomanSuffixes = new(() => LoadSuffixes("man_vs_woman_suffixes.json"));
	private static readonly Lazy<IReadOnlyDictionary<string, string>> WomanFirstVsLastNameSuffixes = new(() => LoadSuffixes("woman_first_vs_last_name_suffixes.json"));

	public static string GetFirstNameVocative(string? firstName, Gender? gender = null) {
		return Vocative(firstName, GenderToWomanBool(gender), false);
	}

	public static string GetLastNameVocative(string? lastName, Gender? gender = null) {
		return Vocative(lastName, GenderToWomanBool(gender), true);
	}

	public static string GetFullNameVocative(string? firstName, string? lastName, Gender? gender = null) {
		var vocativeFirstName = GetFirstNameVocative(firstName, gender);
		var vocativeLastName = GetLastNameVocative(lastName, gender);

		return string.Join(" ", new[] { vocativeFirstName, vocativeLastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
	}

	public static string Vocative(string? name, bool? woman = null, bool? lastName = null) {
		if(string.IsNullOrWhiteSpace(name)) return "";

		var originalName = name.Trim();
		var normalizedName = originalName.ToLower(CzechCulture);
		var isWoman = woman ?? IsWoman(normalizedName);

		if(isWoman) {
			var resolvedLastName = lastName ?? (GetMatchingSuffix(normalizedName, WomanFirstVsLastNameSuffixes.Value).Value == "l");
			return resolvedLastName
				? originalName
				: VocativeWomanFirstName(originalName, normalizedName);
		}

		return VocativeMan(originalName, normalizedName);
	}

	public static bool IsWoman(string? name) {
		if(string.IsNullOrWhiteSpace(name)) return false;

		var normalizedName = name.Trim().ToLower(CzechCulture);
		return GetMatchingSuffix(normalizedName, ManVsWomanSuffixes.Value).Value == "w";
	}

	private static string VocativeWomanFirstName(string originalName, string normalizedName) {
		if(!normalizedName.EndsWith("a", StringComparison.Ordinal)) return originalName;

		var ending = originalName[^1].ToString();
		var suffix = IsUpperCase(ending) ? "O" : "o";
		return $"{originalName[..^1]}{suffix}";
	}

	private static string VocativeMan(string originalName, string normalizedName) {
		var match = GetMatchingSuffix(normalizedName, ManSuffixes.Value);
		var prefixLength = originalName.Length - match.Search.Length;
		var prefix = originalName[..prefixLength];
		var originalSuffix = originalName[prefixLength..];

		return $"{prefix}{MatchCase(originalSuffix, match.Value)}";
	}

	private static SuffixMatch GetMatchingSuffix(string name, IReadOnlyDictionary<string, string> suffixes) {
		for(var start = name.Length; start > 0; start--) {
			var search = name[^start..];
			if(suffixes.TryGetValue(search, out var matchedSuffix))
				return new SuffixMatch(search, matchedSuffix);
		}

		return new SuffixMatch("", suffixes.ValueOrDefault(""));
	}

	private static string MatchCase(string template, string value) {
		if(template.Length == 0) return value;

		var result = new char[value.Length];
		for(var i = 0; i < value.Length; i++) {
			var templateChar = template[Math.Min(i, template.Length - 1)].ToString();
			result[i] = IsUpperCase(templateChar)
				? value[i].ToString().ToUpper(CzechCulture)[0]
				: value[i].ToString().ToLower(CzechCulture)[0];
		}

		return new string(result);
	}

	private static bool IsUpperCase(string value) {
		return value.ToLower(CzechCulture) != value && value.ToUpper(CzechCulture) == value;
	}

	private static bool? GenderToWomanBool(Gender? gender) {
		return gender switch {
			Gender.Female => true,
			Gender.Male => false,
			_ => null,
		};
	}

	private static IReadOnlyDictionary<string, string> LoadSuffixes(string fileName) {
		var path = Path.Combine(AppContext.BaseDirectory, "Services", "VocativeData", fileName);
		var json = File.ReadAllText(path);

		return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
			?? throw new InvalidOperationException($"Could not load Czech vocative suffixes from {path}.");
	}

	private readonly record struct SuffixMatch(string Search, string Value);
}

internal static class CzechVocativeDictionaryExtensions {
	public static string ValueOrDefault(this IReadOnlyDictionary<string, string> dictionary, string key, string fallback = "") {
		return dictionary.TryGetValue(key, out var value) ? value : fallback;
	}
}
