namespace server.Dto;

public sealed class AppSettingsDto
{
    public bool ChatEnabled { get; set; }

    public DateTime ServerNow { get; set; }

    public DateTime ReservationsEnabledFrom { get; set; }

    public DateTime ReservationsEnabledTo { get; set; }

    public string ReservationsStatus { get; set; } = "";

    public bool ReservationsEnabledRightNow { get; set; }
}