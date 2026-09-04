using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace server.Infrastructure.HubRateLimiting;

public sealed class HubRateLimitFilter(
	HubRateLimitManager rateLimitManager,
	ILogger<HubRateLimitFilter> logger
) : IHubFilter {
	public const string DefaultErrorMessage = "Příliš mnoho požadavků. Počkejte prosím chvíli před dalším pokusem.";

	public async ValueTask<object?> InvokeMethodAsync(
		HubInvocationContext invocationContext,
		Func<HubInvocationContext, ValueTask<object?>> next
	) {
		var attribute = invocationContext.HubMethod.GetCustomAttribute<HubRateLimitAttribute>()
			?? invocationContext.Hub.GetType().GetCustomAttribute<HubRateLimitAttribute>();

		if (attribute == null) {
			return await next(invocationContext);
		}

		var clientKey = GetClientKey(invocationContext);
		using var lease = rateLimitManager.AttemptAcquire(attribute.PolicyName, clientKey);

		if (!lease.IsAcquired) {
			logger.LogWarning(
				"SignalR Hub rate limit exceeded for policy '{PolicyName}', method '{HubMethod}', client key '{ClientKey}'.",
				attribute.PolicyName,
				invocationContext.HubMethodName,
				clientKey
			);

			var message = attribute.CustomErrorMessage ?? DefaultErrorMessage;
			await invocationContext.Hub.Clients.Caller.SendAsync("ReceiveError", new { message });
			return null;
		}

		return await next(invocationContext);
	}

	public static string GetClientKey(HubInvocationContext invocationContext) {
		var user = invocationContext.Context.User;
		var userId = user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
			?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
			?? invocationContext.Context.UserIdentifier;

		if (!string.IsNullOrWhiteSpace(userId)) {
			return $"user:{userId}";
		}

		var httpContext = invocationContext.Context.GetHttpContext();
		var ip = ClientInfoExtractor.Extract(httpContext).IpAddress;
		if (!string.IsNullOrWhiteSpace(ip)) {
			return $"ip:{ip}";
		}

		return $"conn:{invocationContext.Context.ConnectionId}";
	}
}
