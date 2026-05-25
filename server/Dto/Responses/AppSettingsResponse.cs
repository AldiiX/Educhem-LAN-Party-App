namespace server.Dto.Responses;

public class AppSettingsResponse {
	public bool ChatEnabled { get; set; }
	public DateTime ServerNow { get; set; }
	public DateTime ReservationsEnabledFrom { get; set; }
	public DateTime ReservationsEnabledTo { get; set; }
	public string ReservationsStatus { get; set; } = "";
	public bool ReservationsEnabledRightNow { get; set; }
	public bool AttendanceEnabled { get; set; }
	public bool ProblemReportsEnabled { get; set; }
}
