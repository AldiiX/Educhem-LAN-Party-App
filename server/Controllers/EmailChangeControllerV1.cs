using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server.Infrastructure;
using server.Services;

namespace server.Controllers;

[ApiController]
[TypeFilter(typeof(AccountWriteExceptionFilter))]
[Route("api/v1/account/email-change")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[EnableRateLimiting("email-change")]
public sealed class EmailChangeControllerV1(EmailChangeService service, IAuthService auth) : ControllerBase {
	[HttpGet, Authorize]
	public Task<IActionResult> Status(CancellationToken ct) => ForAccount(id => service.StatusAsync(id, ct));

	[HttpPost, Authorize]
	public Task<IActionResult> Start(StartRequest request, CancellationToken ct) => ForAccount(id => service.StartAsync(id, request.Email, request.Password, ct));

	[HttpPost("resend"), Authorize]
	public Task<IActionResult> Resend(CancellationToken ct) => ForAccount(id => service.ResendAsync(id, ct));

	[HttpPost("cancel"), Authorize]
	public Task<IActionResult> Cancel(CancellationToken ct) => ForAccount(id => service.CancelAsync(id, ct));

	[HttpPost("preview"), AllowAnonymous]
	public Task<IActionResult> Preview(TokenRequest request, CancellationToken ct) => Run(() => service.PreviewAsync(request.Token, ct));

	[HttpPost("confirm"), AllowAnonymous]
	public Task<IActionResult> Confirm(TokenRequest request, CancellationToken ct) => Run(async () => {
		var result = await service.ConfirmAsync(request.Token, ct);
		if (result.CompletedAccountId is { } id && auth.GetCurrentAccountId() == id) await auth.LogoutAsync(ct);
		return result;
	});

	private Task<IActionResult> ForAccount(Func<Guid, Task<EmailChangeResult>> action) {
		var id = auth.GetCurrentAccountId();
		return id == null ? Task.FromResult<IActionResult>(Unauthorized()) : Run(() => action(id.Value));
	}

	private async Task<IActionResult> Run(Func<Task<EmailChangeResult>> action) {
		var result = await action();
		return result.Error == null ? Ok(result) : StatusCode(result.StatusCode, new { message = result.Error });
	}

	public sealed record StartRequest([Required, MaxLength(254)] string Email, [Required, MaxLength(512)] string Password);
	public sealed record TokenRequest([Required, MaxLength(128)] string Token);
}
