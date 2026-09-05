using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MimeKit;

namespace server.Services;

internal static class EmailService {
	public static async Task<string> RenderAsync<TComponent, TModel>(IServiceProvider services, TModel model)
		where TComponent : IComponent {
		var loggerFactory = services.GetRequiredService<ILoggerFactory>();
		await using var htmlRenderer = new HtmlRenderer(services, loggerFactory);
		return await htmlRenderer.Dispatcher.InvokeAsync(async () => {
			var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> {
				["Model"] = model
			});
			var output = await htmlRenderer.RenderComponentAsync<TComponent>(parameters);
			return output.ToHtmlString();
		});
	}

	private static async Task SendMimeMessageAsync(MimeMessage message) {
		if (!Program.ENV.TryGetValue("SMTP_HOST", out var host) || string.IsNullOrWhiteSpace(host)) {
			throw new InvalidOperationException("SMTP_HOST není nastaven v proměnných prostředí.");
		}

		var portStr = Program.ENV.TryGetValue("SMTP_PORT", out var p) ? p : "465";
		if (!int.TryParse(portStr, out var port)) port = 465;

		var secureSocketOptions = port == 465
			? SecureSocketOptions.SslOnConnect
			: SecureSocketOptions.Auto;

		using var client = new SmtpClient();
		await client.ConnectAsync(host, port, secureSocketOptions);

		if (Program.ENV.TryGetValue("SMTP_EMAIL_USERNAME", out var username) &&
		    Program.ENV.TryGetValue("SMTP_EMAIL_PASSWORD", out var password) &&
		    !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password)) {
			await client.AuthenticateAsync(username, password);
		}

		await client.SendAsync(message);
		await client.DisconnectAsync(quit: true);
	}

	public static async Task<bool> SendPlainTextEmailAsync(string to, string subject, string body) {
		try {
			var senderEmail = Program.ENV.TryGetValue("SMTP_EMAIL_USERNAME", out var username) && !string.IsNullOrWhiteSpace(username)
				? username
				: "noreply@emsio.cz";

			var message = new MimeMessage();
			message.From.Add(new MailboxAddress("EDUCHEM LAN Party", senderEmail));
			message.To.Add(new MailboxAddress(to, to));
			message.Subject = subject;

			message.Body = new TextPart("plain") {
				Text = body
			};

			await SendMimeMessageAsync(message);
			return true;
		} catch (Exception ex) {
			Program.Application.Logger.LogError(ex, "Chyba při odesílání textového emailu ({Subject}) na {To}", subject, to);
			return false;
		}
	}

	public static async Task<bool> SendHtmlEmailAsync<TComponent, TModel>(
		string to,
		string subject,
		TModel model,
		IServiceProvider serviceProvider,
		string? fallbackBody = null
	) where TComponent : IComponent {
		try {
			var senderEmail = Program.ENV.TryGetValue("SMTP_EMAIL_USERNAME", out var username) && !string.IsNullOrWhiteSpace(username)
				? username
				: "noreply@emsio.cz";

			var body = await RenderAsync<TComponent, TModel>(serviceProvider, model);

			var message = new MimeMessage();
			const string name = "EDUCHEM LAN Party";
			message.From.Add(new MailboxAddress(name, senderEmail));
			message.To.Add(new MailboxAddress(to, to));
			message.Subject = subject;
			message.Date = DateTimeOffset.Now;
			message.Headers.Add("Reply-To", senderEmail);
			message.Headers.Add("Return-Path", senderEmail);

			var bodyBuilder = new BodyBuilder { HtmlBody = body };
			if (fallbackBody != null) bodyBuilder.TextBody = fallbackBody;

			message.Body = bodyBuilder.ToMessageBody();

			await SendMimeMessageAsync(message);

			Program.Application.Logger.LogInformation("HTML email ({Subject}) byl úspěšně odeslán na {To}.", subject, to);
			return true;
		} catch (Exception ex) {
			Program.Application.Logger.LogError(ex, "Chyba při odesílání HTML emailu ({Subject}) na {To}", subject, to);
			return false;
		}
	}
}
