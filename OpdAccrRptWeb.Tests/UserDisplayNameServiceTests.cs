using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class UserDisplayNameServiceTests
{
    [Fact]
    public async Task GetDisplayNameAsync_ReturnsNameForAuthenticatedUserId()
    {
        var repository = new StubUserProfileRepository("王小明");
        var service = new UserDisplayNameService(repository, NullLogger<UserDisplayNameService>.Instance);

        string? result = await service.GetDisplayNameAsync("F1482");

        Assert.Equal("王小明", result);
        Assert.Equal("F1482", repository.LastUserId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetDisplayNameAsync_SkipsLookupWithoutUserId(string? userId)
    {
        var repository = new StubUserProfileRepository("王小明");
        var service = new UserDisplayNameService(repository, NullLogger<UserDisplayNameService>.Instance);

        Assert.Null(await service.GetDisplayNameAsync(userId));
        Assert.Null(repository.LastUserId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task GetDisplayNameAsync_ReturnsNullWhenProfileHasNoName(string? profileName)
    {
        var service = new UserDisplayNameService(
            new StubUserProfileRepository(profileName), NullLogger<UserDisplayNameService>.Instance);

        Assert.Null(await service.GetDisplayNameAsync("F1482"));
    }

    private sealed class StubUserProfileRepository(string? name) : IUserProfileRepository
    {
        public string? LastUserId { get; private set; }

        public Task<string?> GetUserNameAsync(string userId, CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            return Task.FromResult(name);
        }
    }
}
