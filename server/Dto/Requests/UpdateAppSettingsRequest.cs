namespace server.Dto.Requests;

public sealed class UpdateAppSettingsRequest
{
    public bool? ChatEnabled { get; set; }

    public DateTime? ReservationsEnabledFrom { get; set; }

    public DateTime? ReservationsEnabledTo { get; set; }

    public string? ReservationsStatus { get; set; }
}