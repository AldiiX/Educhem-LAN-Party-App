using System.Globalization;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Entities;

namespace server.Services;

public sealed class AppSettingsService : IAppSettingsService
{
    private const string DateFormat = "O";
    private const string CacheKeyPrefix = "appsettings:value:";

    private const string ChatEnabledKey = "ChatEnabled";
    private const string ReservationsEnabledFromKey = "ReservationsEnabledFrom";
    private const string ReservationsEnabledToKey = "ReservationsEnabledTo";
    private const string ReservationsStatusKey = "ReservationsStatus";
    private const string ReservationsEnabledRightNowKey = "ReservationsEnabledRightNow";
    private const string AttendanceEnabledKey = "AttendanceEnabled";
    private const string ProblemReportsEnabledKey = "ProblemReportsEnabled";

    private readonly AppDbContext _db;
    private readonly AppCacheService _cache;

    public AppSettingsService(AppDbContext db, AppCacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken ct = default)
    {
        var cacheKey = GetCacheKey(key);
        if (_cache.TryGetValue(cacheKey, out string? cachedValue))
        {
            return cachedValue;
        }

        var value = await _db.AppSettings
            .AsNoTracking()
            .Where(setting => setting.Property == key)
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync(ct);

        if (value != null)
        {
            _cache.Set(cacheKey, value);
        }

        return value;
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
        _cache.Set(GetCacheKey(key), value);
    }

    public async Task<DateTime> GetReservationsEnabledFromAsync(CancellationToken ct = default)
    {
        var value = await GetDateTimeAsync(ReservationsEnabledFromKey, ct);

        return value ?? DateTime.MaxValue;
    }
    
    public async Task<DateTime> GetReservationsEnabledToAsync(CancellationToken ct = default)
    {
        var value = await GetDateTimeAsync(ReservationsEnabledToKey, ct);

        return value ?? DateTime.MaxValue;
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

        await SyncReservationsEnabledRightNowAsync(ct);
    }

    public async Task<IAppSettingsService.ReservationStatusType> GetReservationsStatusAsync(CancellationToken ct = default)
    {
        var value = await GetValueAsync(ReservationsStatusKey, ct);

        if (!string.IsNullOrWhiteSpace(value)
            && Enum.TryParse<IAppSettingsService.ReservationStatusType>(
                value,
                ignoreCase: true,
                out var status))
        {
            return status;
        }

        return IAppSettingsService.ReservationStatusType.Closed;
    }

    public async Task SetReservationsStatusAsync(
        IAppSettingsService.ReservationStatusType value,
        CancellationToken ct = default)
    {
        await SetValueAsync(ReservationsStatusKey, value.ToString(), ct);

        await SyncReservationsEnabledRightNowAsync(ct);
    }

    public async Task<bool> GetChatEnabledAsync(CancellationToken ct = default)
    {
        return await GetBoolAsync(ChatEnabledKey, ct);
    }

    public async Task SetChatEnabledAsync(bool value, CancellationToken ct = default)
    {
        await SetValueAsync(ChatEnabledKey, value.ToString(), ct);
    }

    public async Task<bool> GetAttendanceEnabledAsync(CancellationToken ct = default)
    {
        return await GetBoolAsync(AttendanceEnabledKey, ct);
    }

    public async Task SetAttendanceEnabledAsync(bool value, CancellationToken ct = default)
    {
        await SetValueAsync(AttendanceEnabledKey, value.ToString(), ct);
    }

    public async Task<bool> GetProblemReportsEnabledAsync(CancellationToken ct = default)
    {
        return await GetBoolAsync(ProblemReportsEnabledKey, ct);
    }

    public async Task SetProblemReportsEnabledAsync(bool value, CancellationToken ct = default)
    {
        await SetValueAsync(ProblemReportsEnabledKey, value.ToString(), ct);
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
    
    public async Task<bool> GetReservationsEnabledRightNowStoredAsync(CancellationToken ct = default)
    {
        return await GetBoolAsync(ReservationsEnabledRightNowKey, ct);
    }

    public async Task SetReservationsEnabledRightNowAsync(bool value, CancellationToken ct = default)
    {
        await SetValueAsync(ReservationsEnabledRightNowKey, value.ToString(), ct);
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

    private static string GetCacheKey(string key)
    {
        return $"{CacheKeyPrefix}{key}";
    }
}
