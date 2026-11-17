using PowerPulse.Core.Enums;
using PowerPulse.Core.Models;
using PowerPulse.ML.Forecasting;
using Xunit;

namespace PowerPulse.Web.Tests;

public class WebArchitectureTests
{
    [Fact]
    public void Forecaster_WasmCertified_ProvidesBaselineModel()
    {
        ITimeSeriesForecaster forecaster = new StatisticalForecaster();
        Assert.NotNull(forecaster.ModelName);
        Assert.False(forecaster.IsMlModelActive);

        var tariff = TariffPlan.CreateDefault();
        var readings = new List<EnergyReading>
        {
            new() { Timestamp = DateTimeOffset.UtcNow.AddDays(-2), ConsumptionKwh = 12.0 },
            new() { Timestamp = DateTimeOffset.UtcNow.AddDays(-1), ConsumptionKwh = 10.0 }
        };

        var result = forecaster.ForecastCycleConsumption(readings, tariff);
        Assert.NotNull(result);
        Assert.True(result.ProjectedTotalKwh > 0);
    }

    [Fact]
    public void HouseholdProfile_DefaultState_RequiresOnboarding()
    {
        var profile = new HouseholdProfile();
        Assert.False(profile.IsOnboarded);
        Assert.Equal("Home", profile.Name);
        Assert.Equal("$", profile.Currency);
    }
}
