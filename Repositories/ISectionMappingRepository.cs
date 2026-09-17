using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface ISectionMappingRepository
{
    Task<bool> LocationExistsAsync(string location, CancellationToken cancellationToken = default);
    Task<SectionMapping?> FindPlaceAsync(string code, CancellationToken cancellationToken = default);
    Task<SectionMapping?> FindSectionAsync(string code, CancellationToken cancellationToken = default);
    Task<SectionMapping?> FindLocationAsync(string sectionCode, CancellationToken cancellationToken = default);
    Task<SectionMapping> TranslateAsync(string oldCode, string roomType = "",
        CancellationToken cancellationToken = default);
}
