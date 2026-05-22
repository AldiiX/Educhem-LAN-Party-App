using System.Globalization;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Entities;

namespace server.Services;

public sealed class AppSettingsService : IAppSettingsService
{
    private const string DateFormat = "O";

    private const string ChatEnabledKey = "ChatEnabled";
    private const string ReservationsEnabledFromKey = "ReservationsEnabledFrom";
    private const string ReservationsEnabledToKey = "ReservationsEnabledTo";
    private const string ReservationsStatusKey = "ReservationsStatus";
    private const string ReservationsEnabledRightNowKey = "ReservationsEnabledRightNow";

    private readonly AppDbContext _db;

    private DateTime? _reservationsEnabledFrom;
    private DateTime? _reservationsEnabledTo;
    private IAppSettingsService.ReservationStatusType? _reservationsStatus;
    private bool? _chatEnabled;
    private bool? _reservationsEnabledRightNow;

    public AppSettingsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken ct = default)
    {
        return await _db.AppSettings
            .AsNoTracking()
            .Where(setting => setting.Property == key)
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> GetBoolAsync(string key, CancellationToken ct = default)
    {
        var value = await GetValueAsync(key, ct);

        return bool.TryParse(value, out var result) && result;
    }

    public async Task<DateTime?> GetDateTimeAsync(string key, CancellationToken ct = default)
    {
        var value = await GetValueAsync(key, ct);

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var date))
        {
            if (date.Kind == DateTimeKind.Utc)
            {
                return date;
            }

            if (date.Kind == DateTimeKind.Local)
            {
                return date.ToUniversalTime();
            }

            return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }

        return null;
    }

    public async Task SetValueAsync(string key, string value, CancellationToken ct = default)
    {
        var setting = await _db.AppSettings
            .FirstOrDefaultAsync(x => x.Property == key, ct);

        if (setting == null)
        {
            _db.AppSettings.Add(new AppSettingsItem()
            {
                Property = key,
                Value = value
            });
        }
        else
        {
            setting.Value = value;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<DateTime> GetReservationsEnabledFromAsync(CancellationToken ct = default)
    {
        // if (_reservationsEnabledFrom.HasValue)
        // {
        //     return _reservationsEnabledFrom.Value;
        // }

        var value = await GetDateTimeAsync(ReservationsEnabledFromKey, ct);

        _reservationsEnabledFrom = value ?? DateTime.MaxValue;

        return _reservationsEnabledFrom.Value;
    }
    
    public async Task<DateTime> GetReservationsEnabledToAsync(CancellationToken ct = default)
    {
        // if (_reservationsEnabledTo.HasValue)
        // {
        //     return _reservationsEnabledTo.Value;
        // }

        var value = await GetDateTimeAsync(ReservationsEnabledToKey, ct);

        _reservationsEnabledTo = value ?? DateTime.MaxValue;

        return _reservationsEnabledTo.Value;
    }
    
    public async Task SetReservationsEnabledFromAsync(DateTime value, CancellationToken ct = default)
    {
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();

        await SetValueAsync(
            ReservationsEnabledFromKey,
            utc.ToString(DateFormat, CultureInfo.InvariantCulture),
            ct);

        _reservationsEnabledFrom = utc;

        await SyncReservationsEnabledRightNowAsync(ct);
    }

    public async Task SetReservationsEnabledToAsync(DateTime value, CancellationToken ct = default)
    {
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();

        await SetValueAsync(
            ReservationsEnabledToKey,
            utc.ToString(DateFormat, CultureInfo.InvariantCulture),
            ct);

        _reservationsEnabledTo = utc;

        await SyncReservationsEnabledRightNowAsync(ct);
    }

    public async Task<IAppSettingsService.ReservationStatusType> GetReservationsStatusAsync(CancellationToken ct = default)
    {
        // if (_reservationsStatus.HasValue)
        // {
        //     return _reservationsStatus.Value;
        // }

        var value = await GetValueAsync(ReservationsStatusKey, ct);

        if (!string.IsNullOrWhiteSpace(value)
            && Enum.TryParse<IAppSettingsService.ReservationStatusType>(
                value,
                ignoreCase: true,
                out var status))
        {
            _reservationsStatus = status;
            return status;
        }

        _reservationsStatus = IAppSettingsService.ReservationStatusType.Closed;

        return _reservationsStatus.Value;
    }

    public async Task SetReservationsStatusAsync(
        IAppSettingsService.ReservationStatusType value,
        CancellationToken ct = default)
    {
        await SetValueAsync(ReservationsStatusKey, value.ToString(), ct);

        _reservationsStatus = value;

        await SyncReservationsEnabledRightNowAsync(ct);
    }

    public async Task<bool> GetChatEnabledAsync(CancellationToken ct = default)
    {
        if (_chatEnabled.HasValue)
        {
            return _chatEnabled.Value;
        }

        _chatEnabled = await GetBoolAsync(ChatEnabledKey, ct);

        return _chatEnabled.Value;
    }

    public async Task SetChatEnabledAsync(bool value, CancellationToken ct = default)
    {
        await SetValueAsync(ChatEnabledKey, value.ToString(), ct);

        _chatEnabled = value;
    }

    public async Task<bool> AreReservationsEnabledRightNowAsync(CancellationToken ct = default)
    {
        var status = await GetReservationsStatusAsync(ct);

        if (status == IAppSettingsService.ReservationStatusType.Open)
        {
            return true;
        }

        if (status == IAppSettingsService.ReservationStatusType.Closed)
        {
            return false;
        }

        var from = await GetReservationsEnabledFromAsync(ct);
        var to = await GetReservationsEnabledToAsync(ct);
        var now = DateTime.UtcNow;

        return now >= from && now <= to;
    }
    
    // public async Task<bool> GetReservationsEnabledRightNowStoredAsync(CancellationToken ct = default)
    // {
    //     if (_reservationsEnabledRightNow.HasValue)
    //     {
    //         return _reservationsEnabledRightNow.Value;
    //     }
    //
    //     _reservationsEnabledRightNow = await GetBoolAsync(ReservationsEnabledRightNowKey, ct);
    //
    //     return _reservationsEnabledRightNow.Value;
    // }
    
    public async Task<bool> GetReservationsEnabledRightNowStoredAsync(CancellationToken ct = default)
    {
        _reservationsEnabledRightNow = await GetBoolAsync(ReservationsEnabledRightNowKey, ct);
    
        return _reservationsEnabledRightNow.Value;
    }

    public async Task SetReservationsEnabledRightNowAsync(bool value, CancellationToken ct = default)
    {
        await SetValueAsync(ReservationsEnabledRightNowKey, value.ToString(), ct);

        _reservationsEnabledRightNow = value;
    }

    public async Task<bool> SyncReservationsEnabledRightNowAsync(CancellationToken ct = default)
    {
        var realValue = await AreReservationsEnabledRightNowAsync(ct);
        var storedValue = await GetReservationsEnabledRightNowStoredAsync(ct);

        if (realValue != storedValue)
        {
            await SetReservationsEnabledRightNowAsync(realValue, ct);
        }

        return realValue;
    }
}