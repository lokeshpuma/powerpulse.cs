using PowerPulse.Core.Analytics;
using PowerPulse.Core.Enums;
using PowerPulse.Core.Models;
using Xunit;

namespace PowerPulse.Core.Tests;

public class ForecastingTests
{
    [Fact]
    public void GenerateForecast_CalculatesObservedAndProjectedCycleKwh()
    {
        var tariff = TariffPlan.CreateDefault();
        var readings = new List<EnergyReading>();

        // 10 days of observations in November, starting Nov 1 (10 kWh each day = 100 kWh)
        var startDate = new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
        for (int i = 0; i < 10; i++)
        {
            readings.Add(new EnergyReading
            {
                Timestamp = startDate.AddDays(i),
                ConsumptionKwh = 10.0
            });
        }

        // As of Nov 10, with billing cycle starting on day 1
        var forecast = ForecastingEngine.GenerateForecast(
            readings, tariff, billingCycleStartDay: 1, ForecastMethod.AverageDaily, asOfDate: startDate.AddDays(9));

        Assert.Equal(100.0, forecast.ObservedKwh);
        Assert.Equal(10, forecast.ObservedDays);
        Assert.Equal(10.0, forecast.DailyAverageKwh);
        // In November (30 days total), remaining days = 20
        Assert.Equal(20, forecast.RemainingDays);
        Assert.Equal(200.0, forecast.ExpectedRemainingKwh);
        // Total projected = 100 + 200 = 300 kWh
        Assert.Equal(300.0, forecast.ProjectedTotalKwh);
        Assert.True(forecast.ProjectedBill > 0);
    }
}
