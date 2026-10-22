namespace OpdAccrRptWeb.Services;

public interface IUserDisplayNameService
{
    Task<string?> GetDisplayNameAsync(string? userId, CancellationToken cancellationToken = default);
}
