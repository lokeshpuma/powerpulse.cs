using PowerPulse.Core.Calculations;
using PowerPulse.Core.Models;
using Xunit;

namespace PowerPulse.Core.Tests;

public class CarbonCalculatorTests
{
    [Fact]
    public void CalculateEmissionsKg_ConfiguredFactor_CalculatesCorrectly()
    {
        var factor = new CarbonFactor
        {
            KgCo2ePerKwh = 0.400m,
            IsConfigured = true
        };

        double? emissions = CarbonCalculator.CalculateEmissionsKg(250.0, factor);
        // 250 kWh * 0.400 = 100.0 kg
        Assert.NotNull(emissions);
        Assert.Equal(100.0, emissions.Value);
    }

    [Fact]
    public void CalculateEmissionsKg_WhenNotConfigured_ReturnsNull()
    {
        var factor = new CarbonFactor { IsConfigured = false };
        double? emissions = CarbonCalculator.CalculateEmissionsKg(250.0, factor);
        Assert.Null(emissions);

        double? nullFactorEmissions = CarbonCalculator.CalculateEmissionsKg(250.0, null);
        Assert.Null(nullFactorEmissions);
    }
}
