using PowerPulse.Core.Models;

namespace PowerPulse.Core.Interfaces;

public interface IStorageService
{
    Task InitializeAsync();

    Task<HouseholdProfile> GetProfileAsync();
    Task SaveProfileAsync(HouseholdProfile profile);

    Task<AppSettings> GetSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);

    Task<TariffPlan> GetTariffAsync();
    Task SaveTariffAsync(TariffPlan tariff);

    Task<CarbonFactor> GetCarbonFactorAsync();
    Task SaveCarbonFactorAsync(CarbonFactor factor);

    Task<List<Appliance>> GetAppliancesAsync();
    Task SaveApplianceAsync(Appliance appliance);
    Task DeleteApplianceAsync(string id);
    Task ClearAppliancesAsync();

    Task<List<EnergyDataset>> GetDatasetsAsync();
    Task SaveDatasetAsync(EnergyDataset dataset);
    Task DeleteDatasetAsync(string id);
    Task ClearDatasetsAsync();

    Task<List<EnergyReading>> GetAllReadingsAsync();
    Task<List<EnergyReading>> GetReadingsByDateRangeAsync(DateTimeOffset start, DateTimeOffset end);
    Task SaveReadingsBatchAsync(IEnumerable<EnergyReading> readings);
    Task DeleteReadingsByDatasetAsync(string datasetId);
    Task ClearReadingsAsync();

    Task<List<Recommendation>> GetRecommendationsAsync();
    Task SaveRecommendationsAsync(IEnumerable<Recommendation> recommendations);
    Task MarkRecommendationReviewedAsync(string id);
    Task DismissRecommendationAsync(string id);
    Task ClearRecommendationsAsync();

    Task<string> ExportBackupJsonAsync();
    Task<(bool Success, string Message)> RestoreBackupJsonAsync(string json);
    Task ClearAllDataAsync();
}
