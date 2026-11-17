using System.Text.Json;
using Microsoft.JSInterop;
using PowerPulse.Core.Interfaces;
using PowerPulse.Core.Models;
using PowerPulse.Infrastructure.Backup;

namespace PowerPulse.Infrastructure.Storage;

public class IndexedDbStorageService : IStorageService
{
    private readonly IJSRuntime? _jsRuntime;
    private bool _initialized;

    // In-memory cache and fallback
    private HouseholdProfile _profile = new();
    private AppSettings _settings = new();
    private TariffPlan _tariff = TariffPlan.CreateDefault();
    private CarbonFactor _carbonFactor = CarbonFactor.CreateDefault();
    private readonly List<Appliance> _appliances = new();
    private readonly List<EnergyDataset> _datasets = new();
    private readonly List<EnergyReading> _readings = new();
    private readonly List<Recommendation> _recommendations = new();

    public IndexedDbStorageService(IJSRuntime? jsRuntime = null)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        if (_jsRuntime != null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("powerPulseDb.init");

                // Load cached entities from IndexedDB
                var profile = await _jsRuntime.InvokeAsync<HouseholdProfile?>("powerPulseDb.getProfile");
                if (profile != null) _profile = profile;

                var settings = await _jsRuntime.InvokeAsync<AppSettings?>("powerPulseDb.getSettings");
                if (settings != null) _settings = settings;

                var tariff = await _jsRuntime.InvokeAsync<TariffPlan?>("powerPulseDb.getTariff");
                if (tariff != null) _tariff = tariff;

                var carbon = await _jsRuntime.InvokeAsync<CarbonFactor?>("powerPulseDb.getCarbonFactor");
                if (carbon != null) _carbonFactor = carbon;

                var apps = await _jsRuntime.InvokeAsync<List<Appliance>?>("powerPulseDb.getAppliances");
                if (apps != null && apps.Count > 0)
                {
                    _appliances.Clear();
                    _appliances.AddRange(apps);
                }

                var datasets = await _jsRuntime.InvokeAsync<List<EnergyDataset>?>("powerPulseDb.getDatasets");
                if (datasets != null && datasets.Count > 0)
                {
                    _datasets.Clear();
                    _datasets.AddRange(datasets);
                }

                var readings = await _jsRuntime.InvokeAsync<List<EnergyReading>?>("powerPulseDb.getAllReadings");
                if (readings != null && readings.Count > 0)
                {
                    _readings.Clear();
                    _readings.AddRange(readings);
                }

                var recs = await _jsRuntime.InvokeAsync<List<Recommendation>?>("powerPulseDb.getRecommendations");
                if (recs != null && recs.Count > 0)
                {
                    _recommendations.Clear();
                    _recommendations.AddRange(recs);
                }
            }
            catch
            {
                // Fallback to in-memory mode if JS interop fails (e.g. during test runner)
            }
        }

        _initialized = true;
    }

    public Task<HouseholdProfile> GetProfileAsync() => Task.FromResult(_profile);

    public async Task SaveProfileAsync(HouseholdProfile profile)
    {
        _profile = profile;
        _profile.UpdatedAt = DateTime.UtcNow;
        if (_jsRuntime != null)
        {
            try { await _jsRuntime.InvokeVoidAsync("powerPulseDb.saveProfile", profile); } catch { }
        }
    }

    public Task<AppSettings> GetSettingsAsync() => Task.FromResult(_settings);

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        _settings = settings;
        if (_jsRuntime != null)
        {
            try { await _jsRuntime.InvokeVoidAsync("powerPulseDb.saveSettings", settings); } catch { }
        }
    }

    public Task<TariffPlan> GetTariffAsync() => Task.FromResult(_tariff);

    public async Task SaveTariffAsync(TariffPlan tariff)
    {
        _tariff = tariff;
        if (_jsRuntime != null)
        {
            try { await _jsRuntime.InvokeVoidAsync("powerPulseDb.saveTariff", tariff); } catch { }
        }
    }

    public Task<CarbonFactor> GetCarbonFactorAsync() => Task.FromResult(_carbonFactor);

    public async Task SaveCarbonFactorAsync(CarbonFactor factor)
    {
        _carbonFactor = factor;
        if (_jsRuntime != null)
        {
            try { await _jsRuntime.InvokeVoidAsync("powerPulseDb.saveCarbonFactor", factor); } catch { }
        }
    }

    public Task<List<Appliance>> GetAppliancesAsync() => Task.FromResult(_appliances.ToList());

    public async Task SaveApplianceAsync(Appliance appliance)
    {
        var existing = _appliances.FirstOrDefault(a => a.Id == appliance.Id);
        if (existing != null)
        {
            _appliances.Remove(existing);
        }
        _appliances.Add(appliance);

        if (_jsRuntime != null)
        {
            try { await _jsRuntime.InvokeVoidAsync("powerPulseDb.saveAppliance", appliance); } catch { }
        }
    }

    public async Task DeleteApplianceAsync(string id)
    {
        _appliances.RemoveAll(a => a.Id == id);
        if (_jsRuntime != null)
        {
            try { await _jsRuntime.InvokeVoidAsync("powerPulseDb.deleteAppliance", id); } catch { }
        }
    }

    public async Task ClearAppliancesAsync()
    {
        _appliances.Clear();
        if (_jsRuntime != null)
        {
            try { await _jsRuntime.InvokeVoidAsync("powerPulseDb.clearAppliances"); } catch { }
        }
    }

    public Task<List<EnergyDataset>> GetDatasetsAsync() => Task.FromResult(_datasets.ToList());

    public async Task SaveDatasetAsync(EnergyDataset dataset)
    {
        var existing = _datasets.FirstOrDefault(d => d.Id == dataset.Id);
        if (existing != null)
        {
            _datasets.Remove(existing);
        }
        _datasets.Add(dataset);

        if (_jsRuntime != null)
        {
            try { await _jsRuntime.InvokeVoidAsync("powerPulseDb.saveDataset", dataset); } catch { }
        }
    }

    public async Task DeleteDatasetAsync(string id)
    {
        _datasets.RemoveAll(d => d.Id == id);
        _readings.RemoveAll(r => r.DatasetId == id);

        if (_jsRuntime != null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("powerPulseDb.deleteDataset", id);
                await _jsRuntime.InvokeVoidAsync("powerPulseDb.deleteReadingsByDataset", id);
            }
            catch { }
        }
    }

    public async Task ClearDatasetsAsync()
    {
        _datasets.Clear();
        _readings.Clear();
        if (_jsRuntime != null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("powerPulseDb.clearDatasets");
                await _jsRuntime.InvokeVoidAsync("powerPulseDb.clearReadings");
            }
            catch { }
        }
    }

    public Task<List<EnergyReading>> GetAllReadingsAsync() => Task.FromResult(_readings.OrderBy(r => r.Timestamp).ToList());

    public Task<List<EnergyReading>> GetReadingsByDateRangeAsync(DateTimeOffset start, DateTimeOffset end)
    {
        var filtered = _readings
            .Where(r => r.Timestamp >= start && r.Timestamp <= end)
            .OrderBy(r => r.Timestamp)
            .ToList();
        return Task.FromResult(filtered);
    }

    public async Task SaveReadingsBatchAsync(IEnumerable<EnergyReading> readings)
    {
        var list = readings.ToList();
        _readings.AddRange(list);

        if (_jsRuntime != null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("powerPulseDb.saveReadingsBatch", list);
            }
            catch { }
        }
    }

    public async Task DeleteReadingsByDatasetAsync(string datasetId)
    {
        _readings.RemoveAll(r => r.DatasetId == datasetId);
        if (_jsRuntime != null)
        {
            try { await _jsRuntime.InvokeVoidAsync("powerPulseDb.deleteReadingsByDataset", datasetId); } catch { }
        }
    }

    public async Task ClearReadingsAsync()
    {
        _readings.Clear();
        if (_jsRuntime != null)
        {
            try { await _jsRuntime.InvokeVoidAsync("powerPulseDb.clearReadings"); } catch { }
        }
    }

    public Task<List<Recommendation>> GetRecommendationsAsync() => Task.FromResult(_recommendations.ToList());

    public async Task SaveRecommendationsAsync(IEnumerable<Recommendation> recommendations)
    {
        _recommendations.Clear();
        _recommendations.AddRange(recommendations);

        if (_jsRuntime != null)
        {
            try { await _jsRuntime.InvokeVoidAsync("powerPulseDb.saveRecommendations", _recommendations); } catch { }
        }
    }

    public async Task MarkRecommendationReviewedAsync(string id)
    {
        var rec = _recommendations.FirstOrDefault(r => r.Id == id);
        if (rec != null)
        {
            rec.IsReviewed = true;
            if (_jsRuntime != null)
            {
                try { await _jsRuntime.InvokeVoidAsync("powerPulseDb.saveRecommendations", _recommendations); } catch { }
            }
        }
    }

    public async Task DismissRecommendationAsync(string id)
    {
        var rec = _recommendations.FirstOrDefault(r => r.Id == id);
        if (rec != null)
        {
            rec.IsDismissed = true;
            if (_jsRuntime != null)
            {
                try { await _jsRuntime.InvokeVoidAsync("powerPulseDb.saveRecommendations", _recommendations); } catch { }
            }
        }
    }

    public async Task ClearRecommendationsAsync()
    {
        _recommendations.Clear();
        if (_jsRuntime != null)
        {
            try { await _jsRuntime.InvokeVoidAsync("powerPulseDb.clearRecommendations"); } catch { }
        }
    }

    public Task<string> ExportBackupJsonAsync()
    {
        var backup = new BackupDataModel
        {
            AppVersion = "1.0.0",
            ExportedAt = DateTime.UtcNow,
            Profile = _profile,
            Settings = _settings,
            Tariff = _tariff,
            CarbonFactor = _carbonFactor,
            Appliances = _appliances.ToList(),
            Datasets = _datasets.ToList(),
            Readings = _readings.ToList(),
            Recommendations = _recommendations.ToList()
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        return Task.FromResult(JsonSerializer.Serialize(backup, options));
    }

    public async Task<(bool Success, string Message)> RestoreBackupJsonAsync(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return (false, "Backup data is empty.");

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var backup = JsonSerializer.Deserialize<BackupDataModel>(json, options);

            if (backup == null)
                return (false, "Unable to parse backup data format.");

            if (backup.Profile != null) await SaveProfileAsync(backup.Profile);
            if (backup.Settings != null) await SaveSettingsAsync(backup.Settings);
            if (backup.Tariff != null) await SaveTariffAsync(backup.Tariff);
            if (backup.CarbonFactor != null) await SaveCarbonFactorAsync(backup.CarbonFactor);

            await ClearAppliancesAsync();
            foreach (var a in backup.Appliances)
            {
                await SaveApplianceAsync(a);
            }

            await ClearDatasetsAsync();
            foreach (var d in backup.Datasets)
            {
                await SaveDatasetAsync(d);
            }

            await ClearReadingsAsync();
            if (backup.Readings.Count > 0)
            {
                await SaveReadingsBatchAsync(backup.Readings);
            }

            if (backup.Recommendations.Count > 0)
            {
                await SaveRecommendationsAsync(backup.Recommendations);
            }

            return (true, $"Backup successfully restored ({backup.Readings.Count} readings, {backup.Appliances.Count} appliances).");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to restore backup: {ex.Message}");
        }
    }

    public async Task ClearAllDataAsync()
    {
        _profile = new HouseholdProfile { IsOnboarded = false };
        _settings = new AppSettings();
        _tariff = TariffPlan.CreateDefault();
        _carbonFactor = CarbonFactor.CreateDefault();
        _appliances.Clear();
        _datasets.Clear();
        _readings.Clear();
        _recommendations.Clear();

        if (_jsRuntime != null)
        {
            try { await _jsRuntime.InvokeVoidAsync("powerPulseDb.clearAll"); } catch { }
        }
    }
}
