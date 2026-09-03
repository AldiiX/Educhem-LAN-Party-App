using server.Models;

namespace server.Services;

internal sealed class EmailChangeMailer(IServiceProvider services) : IEmailChangeMailer {
	public Task<bool> SendAsync(EmailChangeMessage message) {
		var model = new EmailChangeModel(message);
		var title = message.Completed ? "E-mail byl změněn" : "Potvrzení změny e-mailu";
		var body = message.Completed
			? $"{model.Greeting}, e-mail účtu byl změněn z {message.OldEmail} na {message.NewEmail}. Přihlášení nyní používá nový e-mail a stejné heslo. Ostatní zařízení se odhlásí po vypršení aktuálního přihlášení, přibližně do 10 minut. Pokud změna nebyla vaše, kontaktujte administrátora."
			: $"{model.Greeting}, změna e-mailu z {message.OldEmail} na {message.NewEmail} čeká na potvrzení obou adres. Platnost do {model.Expires} (Europe/Prague). Potvrzení: {message.ConfirmLink}\nZrušení: {message.CancelLink}\nBez přístupu k původnímu e-mailu kontaktujte administrátora.";
		return EmailService.SendHtmlEmailAsync(message.Recipient, $"EDUCHEM LAN Party - {title}",
			"/Views/Emails/UserEmailChange.cshtml", model, services, body);
	}
}
