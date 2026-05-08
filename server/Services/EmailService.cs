using MailKit.Net.Smtp;
using MimeKit;

namespace server.Services;

public static class EmailService {
	public static async Task<bool> SendPlainTextEmailAsync(string to, string subject, string body) {
		try {
			var message = new MimeMessage();
			message.From.Add(new MailboxAddress("EDUCHEM LAN Party", Program.ENV["SMTP_EMAIL_USERNAME"]));
			message.To.Add(new MailboxAddress(to, to));
			message.Subject = subject;

			message.Body = new TextPart("plain") {
				Text = body
			};

			using var client = new SmtpClient();
			await client.ConnectAsync(Program.ENV["SMTP_HOST"], int.Parse(Program.ENV["SMTP_PORT"]), useSsl: true);
			await client.AuthenticateAsync(Program.ENV["SMTP_EMAIL_USERNAME"], Program.ENV["SMTP_EMAIL_PASSWORD"]);
			await client.SendAsync(message);
			await client.DisconnectAsync(quit: true);
			return true;
		} catch (Exception ex) {
			Program.Application.Logger.LogError(ex, "Error sending plain text email");
			return false;
		}
	}

	public static async Task<bool> SendHtmlEmailAsync<TModel>(
		string to,
		string subject,
		string razorViewName,
		TModel model,
		IServiceProvider serviceProvider,
		string? fallbackBody = null
	) {
		try {
			var body = await RazorEngineService.RenderViewToStringAsync(serviceProvider, razorViewName, model);

			var message = new MimeMessage();
			const string name = "EDUCHEM LAN Party";
			message.From.Add(new MailboxAddress(name, Program.ENV["SMTP_EMAIL_USERNAME"]));
			message.To.Add(new MailboxAddress(to, to));
			message.Subject = subject;
			message.Date = DateTimeOffset.Now;
			message.Headers.Add("Reply-To", Program.ENV["SMTP_EMAIL_USERNAME"]);
			message.Headers.Add("Return-Path", Program.ENV["SMTP_EMAIL_USERNAME"]);

			var bodyBuilder = new BodyBuilder { HtmlBody = body };
			if(fallbackBody != null) bodyBuilder.TextBody = fallbackBody;

			message.Body = bodyBuilder.ToMessageBody();

			using var client = new SmtpClient();
			await client.ConnectAsync(Program.ENV["SMTP_HOST"], int.Parse(Program.ENV["SMTP_PORT"]), useSsl: true);
			await client.AuthenticateAsync(Program.ENV["SMTP_EMAIL_USERNAME"], Program.ENV["SMTP_EMAIL_PASSWORD"]);
			await client.SendAsync(message);
			await client.DisconnectAsync(quit: true);

			Program.Application.Logger.LogDebug("Email sent successfully.");
			return true;
		} catch (Exception ex) {
			Program.Application.Logger.LogError(ex, "Error sending HTML email");
			return false;
		}
	}
}
