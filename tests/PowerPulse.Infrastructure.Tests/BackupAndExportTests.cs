using PowerPulse.Core.Enums;
using PowerPulse.Core.Models;
using PowerPulse.Infrastructure.Export;
using PowerPulse.Infrastructure.Storage;
using Xunit;

namespace PowerPulse.Infrastructure.Tests;

public class BackupAndExportTests
{
    [Fact]
    public void GenerateCsvReport_FormulaInjection_EscapesDangerousLeadingCharacters()
    {
        var dateRange = new DateRangeFilter
        {
            StartDate = DateTimeOffset.UtcNow.AddDays(-7),
            EndDate = DateTimeOffset.UtcNow
        };
        var comparison = new PeriodComparisonResult { CurrentTotalKwh = 50.0 };
        var dailyData = new List<DailyEnergyAggregation>();

        // Appliance with dangerous spreadsheet formula names
        var appliances = new List<Appliance>
        {
            new() { Name = "=cmd|' /C calc'!A0", Category = ApplianceCategory.Other, RatedWatts = 100, HoursPerDay = 2, IncludeInCalculation = true },
            new() { Name = "+123456", Category = ApplianceCategory.Other, RatedWatts = 100, HoursPerDay = 2, IncludeInCalculation = true },
            new() { Name = "@SUM(A1:B1)", Category = ApplianceCategory.Other, RatedWatts = 100, HoursPerDay = 2, IncludeInCalculation = true }
        };

        var tariff = TariffPlan.CreateDefault();

        string csv = ReportExportService.GenerateCsvReport(
            dateRange, comparison, dailyData, appliances, new List<AnomalyResult>(), null, new List<Recommendation>(), tariff, null);

        // Verify that formulas starting with =, +, -, @ were prepended with single quote '
        Assert.Contains("\"'=cmd|' /C calc'!A0\"", csv);
        Assert.Contains("\"'+123456\"", csv);
        Assert.Contains("\"'@SUM(A1:B1)\"", csv);
    }

    [Fact]
    public async Task BackupAndRestore_FullLifecycle_RestoresStateSuccessfully()
    {
        var storage = new IndexedDbStorageService(null);
        await storage.InitializeAsync();

        // 1. Setup profile and an appliance
        var profile = new HouseholdProfile { Name = "Test Estate", Currency = "€", IsOnboarded = true };
        await storage.SaveProfileAsync(profile);

        var appliance = new Appliance
        {
            Id = "app-123",
            Name = "Heat Pump",
            RatedWatts = 2500,
            HoursPerDay = 8,
            IncludeInCalculation = true
        };
        await storage.SaveApplianceAsync(appliance);

        // 2. Export Backup
        string json = await storage.ExportBackupJsonAsync();
        Assert.NotEmpty(json);
        Assert.Contains("Test Estate", json);
        Assert.Contains("Heat Pump", json);

        // 3. Clear Storage
        await storage.ClearAllDataAsync();
        var clearedProfile = await storage.GetProfileAsync();
        var clearedApps = await storage.GetAppliancesAsync();
        Assert.False(clearedProfile.IsOnboarded);
        Assert.Empty(clearedApps);

        // 4. Restore Backup
        var (success, msg) = await storage.RestoreBackupJsonAsync(json);
        Assert.True(success);

        var restoredProfile = await storage.GetProfileAsync();
        var restoredApps = await storage.GetAppliancesAsync();

        Assert.Equal("Test Estate", restoredProfile.Name);
        Assert.Equal("€", restoredProfile.Currency);
        Assert.Single(restoredApps);
        Assert.Equal("Heat Pump", restoredApps[0].Name);
    }
}
