using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class C141EntryDateTicketServiceTests
{
    [Fact]
    public void TryRead_BindsTicketToAuthenticatedUserAndEntryDate()
    {
        var timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 10, 13, 8, 0, 0, TimeSpan.Zero));
        var service = new C141EntryDateTicketService(new TestDataProtectionProvider(), timeProvider);
        string ticket = service.Create(new DateOnly(2026, 10, 13), "user-a");

        Assert.True(service.TryRead(ticket, "user-a", out DateOnly entryDate));
        Assert.Equal(new DateOnly(2026, 10, 13), entryDate);
        Assert.False(service.TryRead(ticket, "user-b", out _));
        Assert.False(service.TryRead("invalid", "user-a", out _));
    }

    [Fact]
    public void TryRead_RejectsAnEntryTicketOlderThanOneDay()
    {
        var timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 10, 13, 8, 0, 0, TimeSpan.Zero));
        var service = new C141EntryDateTicketService(new TestDataProtectionProvider(), timeProvider);
        string ticket = service.Create(new DateOnly(2026, 10, 12), "user-a");
        timeProvider.Now = timeProvider.Now.AddHours(25);

        Assert.False(service.TryRead(ticket, "user-a", out _));
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class TestDataProtectionProvider : IDataProtectionProvider
    {
        public IDataProtector CreateProtector(string purpose) => new TestDataProtector();
    }

    private sealed class TestDataProtector : IDataProtector
    {
        public IDataProtector CreateProtector(string purpose) => this;
        public byte[] Protect(byte[] plaintext) => Encoding.UTF8.GetBytes(Protect(Encoding.UTF8.GetString(plaintext)));
        public byte[] Unprotect(byte[] protectedData) => Encoding.UTF8.GetBytes(Unprotect(Encoding.UTF8.GetString(protectedData)));
        public string Protect(string plaintext) => Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext));
        public string Unprotect(string protectedData)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(protectedData));
            }
            catch (FormatException exception)
            {
                throw new CryptographicException("Invalid test ticket.", exception);
            }
        }
    }
}
