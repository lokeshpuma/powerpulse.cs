using PowerPulse.Core.Calculations;
using PowerPulse.Core.Enums;
using PowerPulse.Core.Models;
using Xunit;

namespace PowerPulse.Core.Tests;

public class TariffCalculatorTests
{
    [Fact]
    public void CalculateBill_FlatTariff_CalculatesEnergyAndTax()
    {
        var tariff = new TariffPlan
        {
            Type = TariffType.Flat,
            FlatRatePerKwh = 0.20m,
            FixedCharge = 10.00m,
            OtherCharges = 2.00m,
            TaxPercentage = 10.0m, // 10%
            Currency = "$"
        };

        // 100 kWh * $0.20 = $20.00 Energy Charge
        // Subtotal = $20.00 + $10.00 + $2.00 = $32.00
        // Tax = $32.00 * 10% = $3.20
        // Total = $35.20
        var bill = TariffCalculator.CalculateBill(tariff, 100.0);

        Assert.Equal(20.00m, bill.EnergyCharges);
        Assert.Equal(10.00m, bill.FixedCharges);
        Assert.Equal(2.00m, bill.OtherCharges);
        Assert.Equal(32.00m, bill.Subtotal);
        Assert.Equal(3.20m, bill.TaxAmount);
        Assert.Equal(35.20m, bill.TotalEstimatedBill);
    }

    [Fact]
    public void CalculateSlabCharge_ProgressiveTiers_AppliesRatesCorrectly()
    {
        var slabs = new List<TariffSlab>
        {
            new() { SlabIndex = 1, FromKwh = 0, ToKwh = 100, RatePerKwh = 0.10m },
            new() { SlabIndex = 2, FromKwh = 100, ToKwh = 300, RatePerKwh = 0.15m },
            new() { SlabIndex = 3, FromKwh = 300, ToKwh = null, RatePerKwh = 0.20m }
        };

        // For 350 kWh:
        // Slab 1 (0 to 100): 100 kWh * $0.10 = $10.00
        // Slab 2 (100 to 300): 200 kWh * $0.15 = $30.00
        // Slab 3 (300 to 350): 50 kWh * $0.20 = $10.00
        // Total Energy Charge = $50.00
        var breakdowns = new List<SlabChargeBreakdown>();
        decimal charge = TariffCalculator.CalculateSlabCharge(slabs, 350.0, breakdowns);

        Assert.Equal(50.00m, charge);
        Assert.Equal(3, breakdowns.Count);
        Assert.Equal(100.0, breakdowns[0].KwhInSlab);
        Assert.Equal(200.0, breakdowns[1].KwhInSlab);
        Assert.Equal(50.0, breakdowns[2].KwhInSlab);
    }

    [Fact]
    public void CalculateTimeOfUseCharge_IntervalReadings_CategorizesBands()
    {
        var tariff = TariffPlan.CreateDefault();
        tariff.Type = TariffType.TimeOfUse;

        // Create readings:
        // Hour 3 (Off-peak, $0.08): 10 kWh -> $0.80
        // Hour 16 (Peak, $0.24 on Mon-Fri): 10 kWh -> $2.40
        var monday = new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.Zero); // Nov 2 2026 is Monday
        var readings = new List<EnergyReading>
        {
            new() { Timestamp = monday.AddHours(3), ConsumptionKwh = 10.0 },
            new() { Timestamp = monday.AddHours(16), ConsumptionKwh = 10.0 }
        };

        var breakdowns = new List<TimeOfUseChargeBreakdown>();
        decimal charge = TariffCalculator.CalculateTimeOfUseCharge(tariff, 20.0, readings, breakdowns);

        // Expected: $0.80 + $2.40 = $3.20
        Assert.Equal(3.20m, charge);
        Assert.Contains(breakdowns, b => b.BandName == "Off-Peak");
        Assert.Contains(breakdowns, b => b.BandName == "Peak");
    }
}
