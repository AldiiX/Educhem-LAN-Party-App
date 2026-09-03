using server.Data.Entities;
using server.Services;

namespace server.Models;

public sealed class EmailChangeModel(EmailChangeMessage message) {
	public EmailChangeMessage Message { get; } = message;
	public bool IsFormal => Message.Account.CommunicationStyle == CommunicationStyle.Formal;
	public string Greeting => IsFormal
		? $"Dobrý den, {CzechVocativeService.GetFullNameVocative(Message.Account.FirstName, Message.Account.LastName, Message.Account.Gender)}"
		: $"Ahoj {CzechVocativeService.GetFirstNameVocative(Message.Account.FirstName, Message.Account.Gender)}";
	public string Expires => TimeZoneInfo.ConvertTimeFromUtc(Message.ExpiresAtUtc,
		TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague")).ToString("d. M. yyyy HH:mm");
}
