using Microsoft.AspNetCore.Antiforgery;

namespace server.Infrastructure;

public sealed class AntiforgeryValidationMiddleware(RequestDelegate next) {
	public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery) {
		if (RequiresValidation(context.Request)) {
			try {
				await antiforgery.ValidateRequestAsync(context);
			} catch (AntiforgeryValidationException) {
				context.Response.StatusCode = StatusCodes.Status400BadRequest;
				context.Response.Headers.Append("X-CSRF-Invalid", "1");
				await context.Response.WriteAsJsonAsync(new {
					title = "Neplatny CSRF token.",
					status = StatusCodes.Status400BadRequest,
				});
				return;
			}
		}

		await next(context);
	}

	private static bool RequiresValidation(HttpRequest request) {
		if (!request.Path.StartsWithSegments("/api")) return false;
		if (request.Path.StartsWithSegments("/api/v1/auth")) return false;
		if (request.Path.Equals("/api/v1/account/forgot-password")) return false;
		if (request.Path.StartsWithSegments("/api/v1/account/reset-password")) return false;
		if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)
			|| HttpMethods.IsOptions(request.Method) || HttpMethods.IsTrace(request.Method)) return false;

		var authorization = request.Headers.Authorization.ToString();
		return !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
			&& !authorization.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase);
	}
}
