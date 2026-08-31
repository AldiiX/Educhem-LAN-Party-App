using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace server.Infrastructure;

public static class AuthSchemes {
	public const string AccessToken = "AccessToken";
	public const string ExternalCookie = "ExternalCookie";

	// nachystany pro budouci samostatny API key handler
	public const string ApiKey = "ApiKey";
}

public static class AuthPolicies {
	public const string Teacher = "TeacherOrHigher";
	public const string TeacherOrg = "TeacherOrgOrHigher";
	public const string Admin = "AdminOrHigher";
	public const string SuperAdmin = "SuperAdmin";
}

public sealed class JwtAuthConfiguration {
	public const string Issuer = "educhemlanparty-api";
	public const string Audience = "educhemlanparty-clients";
	public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(10);
	public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

	public SymmetricSecurityKey SigningKey { get; }
	public SigningCredentials SigningCredentials { get; }

	private JwtAuthConfiguration(byte[] secret) {
		SigningKey = new SymmetricSecurityKey(secret);
		SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256);
	}

	public static JwtAuthConfiguration FromEnvironment(IReadOnlyDictionary<string, string> environment) {
		if (!environment.TryGetValue("JWT_SECRET", out var encodedSecret) || string.IsNullOrWhiteSpace(encodedSecret)) {
			throw new InvalidOperationException("JWT_SECRET musi byt nastavene v server/.env.");
		}

		byte[] secret;
		try {
			secret = Convert.FromBase64String(encodedSecret);
		} catch (FormatException exception) {
			throw new InvalidOperationException("JWT_SECRET musi byt Base64 hodnota.", exception);
		}

		if (secret.Length < 32) {
			CryptographicOperations.ZeroMemory(secret);
			throw new InvalidOperationException("JWT_SECRET musi mit po dekodovani aspon 32 bajtu.");
		}

		return new JwtAuthConfiguration(secret);
	}
}

public static class AuthCookieNames {
	public const string DevelopmentAccess = "edlp_access";
	public const string ProductionAccess = "__Host-edlp_access";
	public const string DevelopmentRefresh = "edlp_refresh";
	public const string ProductionRefresh = "__Host-edlp_refresh";
	public const string DevelopmentCsrf = "edlp_csrf";
	public const string ProductionCsrf = "__Host-edlp_csrf";
	public const string DevelopmentAntiforgery = "edlp_antiforgery";
	public const string ProductionAntiforgery = "__Host-edlp_antiforgery";

	public static string Access => Program.DevelopmentMode ? DevelopmentAccess : ProductionAccess;
	public static string Refresh => Program.DevelopmentMode ? DevelopmentRefresh : ProductionRefresh;
	public static string Csrf => Program.DevelopmentMode ? DevelopmentCsrf : ProductionCsrf;
	public static string Antiforgery => Program.DevelopmentMode ? DevelopmentAntiforgery : ProductionAntiforgery;
}
