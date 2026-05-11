namespace server.Models;

public class EmailPasswordResetLinkModel(string resetLink, string email) {
	public string ResetLink { get; set; } = resetLink;
	public string Email { get; set; } = email;
}
