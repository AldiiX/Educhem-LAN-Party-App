using server.Data.Entities;
using server.Services;

namespace server.Models;

public class EmailUserRegisterModel(string passwordNonEncrypted, string webLink, string email, string? firstName = null, string? lastName = null, Gender? gender = null, CommunicationStyle communicationStyle = CommunicationStyle.Formal) {
	public string PasswordNonEncrypted { get; set; } = passwordNonEncrypted;
	public string WebLink { get; set; } = webLink;
	public string Email { get; set; } = email;
	public string FirstName { get; set; } = firstName ?? "";
	public string LastName { get; set; } = lastName ?? "";
	public CommunicationStyle CommunicationStyle { get; set; } = communicationStyle;
	public string VocativeName { get; set; } = CzechVocativeService.GetFirstNameVocative(firstName, gender);
	public string VocativeFullName { get; set; } = CzechVocativeService.GetFullNameVocative(firstName, lastName, gender);
	public string GreetingName => string.IsNullOrWhiteSpace(VocativeName) ? "" : $"{VocativeName}";
	public bool IsFormal => CommunicationStyle == CommunicationStyle.Formal;
	public string Greeting => IsFormal
		? string.IsNullOrWhiteSpace(VocativeFullName) ? "Dobrý den" : $"Dobrý den, {VocativeFullName}"
		: string.IsNullOrWhiteSpace(VocativeName) ? "Ahoj" : $"Ahoj {VocativeName}";
}
