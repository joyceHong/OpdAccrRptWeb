namespace OpdAccrRptWeb.Repositories;

public interface IUserProfileRepository
{
    Task<string?> GetUserNameAsync(string userId, CancellationToken cancellationToken = default);
}
