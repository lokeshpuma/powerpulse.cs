using PowerPulse.Core.Calculations;
using PowerPulse.Core.Enums;
using PowerPulse.Core.Models;
using Xunit;

namespace PowerPulse.Core.Tests;

public class ApplianceCalculatorTests
{
    [Fact]
    public void CalculateDailyKwh_StandardAppliance_CalculatesCorrectly()
    {
        // 1000W heater, 1 qty, 4 hours per day, 7 days/week, duty cycle 1.0
        var appliance = new Appliance
        {
            RatedWatts = 1000,
            Quantity = 1,
            HoursPerDay = 4,
            UsageDaysPerWeek = 7,
            DutyCycle = 1.0,
            IncludeInCalculation = true
        };

        double dailyKwh = ApplianceCalculator.CalculateDailyKwh(appliance);
        // (1000 * 1 * 4 * 1.0) / 1000 = 4.0 kWh
        Assert.Equal(4.0, dailyKwh);
    }

    [Fact]
    public void CalculatePeriodKwh_WithDutyCycleAndStandby_CalculatesAccurately()
    {
        // AC: 1500W, 1 qty, 8 hours/day, 7 days/wk, duty cycle 0.50
        // Standby: 10W, 16 hours/day
        // Period: 30 days
        var appliance = new Appliance
        {
            RatedWatts = 1500,
            Quantity = 1,
            HoursPerDay = 8,
            UsageDaysPerWeek = 7,
            DutyCycle = 0.50,
            StandbyWatts = 10,
            StandbyHoursPerDay = 16,
            IncludeInCalculation = true
        };

        // Active Energy = 1500 * 1 * 8 * 30 * 0.5 / 1000 = 180.0 kWh
        // Standby Energy = 10 * 1 * 16 * 30 / 1000 = 4.8 kWh
        // Total = 184.8 kWh
        double totalKwh = ApplianceCalculator.CalculatePeriodKwh(appliance, 30);
        Assert.Equal(184.8, totalKwh, precision: 2);
    }

    [Fact]
    public void CalculatePeriodKwh_WhenExcluded_ReturnsZero()
    {
        var appliance = new Appliance
        {
            RatedWatts = 2000,
            Quantity = 1,
            HoursPerDay = 5,
            IncludeInCalculation = false
        };

        double totalKwh = ApplianceCalculator.CalculatePeriodKwh(appliance, 30);
        Assert.Equal(0.0, totalKwh);
    }

    [Fact]
    public void EstimateAppliancePeriodCost_CalculatesCorrectCost()
    {
        var appliance = new Appliance
        {
            RatedWatts = 500,
            Quantity = 2,
            HoursPerDay = 5,
            UsageDaysPerWeek = 7,
            DutyCycle = 1.0,
            IncludeInCalculation = true
        };

        // 500W * 2 * 5h * 30d / 1000 = 150 kWh
        // 150 kWh * $0.20 = $30.00
        decimal cost = ApplianceCalculator.EstimateAppliancePeriodCost(appliance, 30, 0.20m);
        Assert.Equal(30.00m, cost);
    }

    [Fact]
    public void CompareWithMeter_CalculatesUnallocatedDifference()
    {
        var appliances = new List<Appliance>
        {
            new() { RatedWatts = 1000, Quantity = 1, HoursPerDay = 2, UsageDaysPerWeek = 7, DutyCycle = 1.0, IncludeInCalculation = true }
        };
        // 1000W * 1 * 2h * 30d / 1000 = 60 kWh

        double meterKwh = 100.0;
        var (measured, estimated, diff, isOver) = ApplianceCalculator.CompareWithMeter(meterKwh, appliances, 30);

        Assert.Equal(100.0, measured);
        Assert.Equal(60.0, estimated);
        Assert.Equal(40.0, diff);
        Assert.False(isOver);
    }
}
