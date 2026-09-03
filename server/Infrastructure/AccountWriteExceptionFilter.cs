using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace server.Infrastructure;

public sealed class AccountWriteExceptionFilter : IExceptionFilter {
	public void OnException(ExceptionContext context) {
		var message = context.Exception switch {
			DbUpdateConcurrencyException => "Účet se mezitím změnil. Obnovte stránku a zkuste to znovu.",
			DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Accounts_Email" } }
				=> "Tuto adresu nelze použít.",
			_ => null,
		};
		if (message == null) return;
		context.Result = new ConflictObjectResult(new { message });
		context.ExceptionHandled = true;
	}
}
