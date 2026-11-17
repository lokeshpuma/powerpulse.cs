using PowerPulse.Core.Analytics;
using PowerPulse.Core.Enums;
using PowerPulse.Core.Models;
using Xunit;

namespace PowerPulse.Core.Tests;

public class RecommendationEngineTests
{
    [Fact]
    public void GenerateRecommendations_EssentialAppliance_NeverSuggestedToSwitchOff()
    {
        var tariff = TariffPlan.CreateDefault();
        var baseDate = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var readings = new List<EnergyReading>();

        for (int i = 0; i < 30; i++)
        {
            readings.Add(new EnergyReading
            {
                Timestamp = baseDate.AddDays(i),
                ConsumptionKwh = 10.0 // 300 kWh total
            });
        }

        // Refrigerator is Essential and takes >30% of power
        var appliances = new List<Appliance>
        {
            new()
            {
                Name = "Medical Life-Support / Fridge",
                Category = ApplianceCategory.Refrigerator,
                RatedWatts = 250,
                HoursPerDay = 24,
                DutyCycle = 1.0,
                IsEssential = true,
                IncludeInCalculation = true
            }
        };

        var dateRange = EnergyAnalyticsEngine.ResolveDateRange(DateRangeOption.Last30Days, baseDate.AddDays(29));
        var recs = RecommendationEngine.GenerateRecommendations(readings, appliances, tariff, dateRange);

        // HighApplianceUsage recommendation should NOT target the essential appliance
        var highAppRec = recs.FirstOrDefault(r => r.Type == RecommendationType.HighApplianceUsage && r.Title.Contains("Medical"));
        Assert.Null(highAppRec);
    }

    [Fact]
    public void GenerateRecommendations_NonEssentialMajorAppliance_TriggersRecommendation()
    {
        var tariff = TariffPlan.CreateDefault();
        var baseDate = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var readings = new List<EnergyReading>();

        for (int i = 0; i < 30; i++)
        {
            readings.Add(new EnergyReading
            {
                Timestamp = baseDate.AddDays(i),
                ConsumptionKwh = 10.0 // 300 kWh total
            });
        }

        // Space Heater consuming 1500W * 6h * 30d / 1000 = 270 kWh (>30% of 300 kWh)
        var appliances = new List<Appliance>
        {
            new()
            {
                Name = "Portable Space Heater",
                Category = ApplianceCategory.Other,
                RatedWatts = 1500,
                HoursPerDay = 6,
                DutyCycle = 1.0,
                IsEssential = false,
                IncludeInCalculation = true
            }
        };

        var dateRange = EnergyAnalyticsEngine.ResolveDateRange(DateRangeOption.Last30Days, baseDate.AddDays(29));
        var recs = RecommendationEngine.GenerateRecommendations(readings, appliances, tariff, dateRange);

        var heaterRec = recs.FirstOrDefault(r => r.Type == RecommendationType.HighApplianceUsage);
        Assert.NotNull(heaterRec);
        Assert.Contains("Space Heater", heaterRec.Title);
    }
}
