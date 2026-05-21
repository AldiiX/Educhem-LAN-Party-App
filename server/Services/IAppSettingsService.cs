namespace server.Services;

public interface IAppSettingsService
{
    public enum ReservationStatusType
    {
        UseTimer,
        Open,
        Closed
    }

    Task<string?> GetValueAsync(string key, CancellationToken ct = default);
    Task<bool> GetBoolAsync(string key, CancellationToken ct = default);
    Task<DateTime?> GetDateTimeAsync(string key, CancellationToken ct = default);
    Task SetValueAsync(string key, string value, CancellationToken ct = default);

    Task<DateTime> GetReservationsEnabledFromAsync(CancellationToken ct = default);
    Task SetReservationsEnabledFromAsync(DateTime value, CancellationToken ct = default);

    Task<DateTime> GetReservationsEnabledToAsync(CancellationToken ct = default);
    Task SetReservationsEnabledToAsync(DateTime value, CancellationToken ct = default);

    Task<ReservationStatusType> GetReservationsStatusAsync(CancellationToken ct = default);
    Task SetReservationsStatusAsync(ReservationStatusType value, CancellationToken ct = default);

    Task<bool> GetChatEnabledAsync(CancellationToken ct = default);
    Task SetChatEnabledAsync(bool value, CancellationToken ct = default);

    Task<bool> AreReservationsEnabledRightNowAsync(CancellationToken ct = default);
}