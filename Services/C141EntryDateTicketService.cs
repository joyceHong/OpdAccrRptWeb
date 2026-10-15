using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace OpdAccrRptWeb.Services;

public sealed class C141EntryDateTicketService : IC141EntryDateTicketService
{
    private static readonly TimeSpan MaximumAge = TimeSpan.FromHours(24);
    private readonly IDataProtector _protector;
    private readonly TimeProvider _timeProvider;

    public C141EntryDateTicketService(IDataProtectionProvider provider, TimeProvider timeProvider)
    {
        _protector = provider.CreateProtector("OpdAccrRptWeb.C141.EntryDate.v1");
        _timeProvider = timeProvider;
    }

    public string Create(DateOnly entryDate, string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return _protector.Protect(JsonSerializer.Serialize(new TicketPayload(
            userId,
            entryDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            _timeProvider.GetUtcNow().ToUnixTimeSeconds())));
    }

    public bool TryRead(string? ticket, string userId, out DateOnly entryDate)
    {
        entryDate = default;
        if (string.IsNullOrWhiteSpace(ticket) || string.IsNullOrWhiteSpace(userId))
            return false;

        try
        {
            TicketPayload? payload = JsonSerializer.Deserialize<TicketPayload>(_protector.Unprotect(ticket));
            DateTimeOffset createdAt = payload is null
                ? DateTimeOffset.MinValue
                : DateTimeOffset.FromUnixTimeSeconds(payload.IssuedAtUnixSeconds);
            if (payload is null
                || !string.Equals(payload.UserId, userId, StringComparison.Ordinal)
                || _timeProvider.GetUtcNow() - createdAt > MaximumAge
                || createdAt - _timeProvider.GetUtcNow() > TimeSpan.FromMinutes(2)
                || !DateOnly.TryParseExact(
                    payload.EntryDate,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out entryDate))
                return false;

            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private sealed record TicketPayload(string UserId, string EntryDate, long IssuedAtUnixSeconds);
}
