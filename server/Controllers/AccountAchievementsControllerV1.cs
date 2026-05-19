using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Dto.Mappers;
using server.Infrastructure;
using server.Services;

namespace server.Controllers;

[ApiController]
[Route("api/v1/account")]
public sealed class AccountAchievementsControllerV1(
	IAuthService auth,
	AppDbContext db,
	ReservationCacheService reservationCache
) : Controller {

	[HttpPut("me/achievements/{id:guid}")]
	public async Task<IActionResult> UpdateMyAchievementVisibility(Guid id, [FromBody] AccountAchievementVisibilityRequest request, CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();

		var entry = await db.AccountAchievementsEf()
			.FirstOrDefaultAsync(x => x.Id == id && x.AccountId == acc.Id, ct);
		if(entry == null) return NotFound();

		entry.IsHidden = request.IsHidden;
		await db.SaveChangesAsync(ct);
		reservationCache.InvalidateReservations();

		var updated = await db.AccountsEf().AsNoTracking().FirstAsync(a => a.Id == acc.Id, ct);
		return Ok(updated.ToDto());
	}

	[HttpPut("me/badges/{id:guid}")]
	public async Task<IActionResult> UpdateMyBadgeTakenOut(Guid id, [FromBody] AccountBadgeTakeoutRequest request, CancellationToken ct = default) {
		var acc = await auth.ReAuthAsync(ct);
		if(acc == null) return new UnauthorizedResult();

		var entry = await db.AccountBadgesEf()
			.FirstOrDefaultAsync(x => x.Id == id && x.AccountId == acc.Id, ct);
		if(entry == null) return NotFound();

		if(request.IsTakenOut && !entry.IsTakenOut) {
			var takenOutCount = await db.AccountBadges
				.AsNoTracking()
				.CountAsync(x => x.AccountId == acc.Id && x.IsTakenOut, ct);
			if(takenOutCount >= 3) return BadRequest("Na profilu mohou byt maximalne 3 badge.");
		}

		entry.IsTakenOut = request.IsTakenOut;
		await db.SaveChangesAsync(ct);
		reservationCache.InvalidateReservations();

		var updated = await db.AccountsEf().AsNoTracking().FirstAsync(a => a.Id == acc.Id, ct);
		return Ok(updated.ToDto());
	}

	public sealed record AccountAchievementVisibilityRequest(bool IsHidden);
	public sealed record AccountBadgeTakeoutRequest(bool IsTakenOut);
}
