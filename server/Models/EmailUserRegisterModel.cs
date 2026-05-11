using server.Data.Entities;
using server.Services;

namespace server.Models;

public class EmailUserRegisterModel(string passwordNonEncrypted, string webLink, string email, string? firstName = null, string? lastName = null, Gender? gender = null) {
	public string PasswordNonEncrypted { get; set; } = passwordNonEncrypted;
	public string WebLink { get; set; } = webLink;
	public string Email { get; set; } = email;
	public string FirstName { get; set; } = firstName ?? "";
	public string LastName { get; set; } = lastName ?? "";
	public string VocativeName { get; set; } = CzechVocativeService.GetFirstNameVocative(firstName, gender);
	public string VocativeFullName { get; set; } = CzechVocativeService.GetFullNameVocative(firstName, lastName, gender);
	public string GreetingName => string.IsNullOrWhiteSpace(VocativeName) ? "" : $"{VocativeName}";
}
