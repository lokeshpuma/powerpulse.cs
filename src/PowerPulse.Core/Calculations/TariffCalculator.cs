using PowerPulse.Core.Enums;
using PowerPulse.Core.Models;

namespace PowerPulse.Core.Calculations;

public static class TariffCalculator
{
    public static BillEstimate CalculateBill(
        TariffPlan tariff,
        double totalKwh,
        IEnumerable<EnergyReading>? intervalReadings = null,
        decimal? actualBill = null,
        DateTimeOffset? periodStart = null,
        DateTimeOffset? periodEnd = null)
    {
        var estimate = new BillEstimate
        {
            BillingPeriodStart = periodStart ?? DateTimeOffset.UtcNow.AddDays(-30),
            BillingPeriodEnd = periodEnd ?? DateTimeOffset.UtcNow,
            TotalKwh = Math.Max(0.0, totalKwh),
            Currency = tariff.Currency,
            FixedCharges = tariff.FixedCharge,
            OtherCharges = tariff.OtherCharges,
            TaxPercentage = tariff.TaxPercentage,
            ActualImportedBill = actualBill
        };

        decimal energyCharge = 0m;

        switch (tariff.Type)
        {
            case TariffType.Flat:
                energyCharge = Math.Round((decimal)estimate.TotalKwh * tariff.FlatRatePerKwh, 2, MidpointRounding.AwayFromZero);
                break;

            case TariffType.Slab:
                energyCharge = CalculateSlabCharge(tariff.Slabs, estimate.TotalKwh, estimate.SlabBreakdowns);
                break;

            case TariffType.TimeOfUse:
                energyCharge = CalculateTimeOfUseCharge(tariff, estimate.TotalKwh, intervalReadings, estimate.TimeOfUseBreakdowns);
                break;
        }

        estimate.EnergyCharges = energyCharge;
        estimate.Subtotal = estimate.EnergyCharges + estimate.FixedCharges + estimate.OtherCharges;
        estimate.TaxAmount = Math.Round(estimate.Subtotal * (tariff.TaxPercentage / 100.0m), 2, MidpointRounding.AwayFromZero);
        estimate.TotalEstimatedBill = estimate.Subtotal + estimate.TaxAmount;

        return estimate;
    }

    public static decimal CalculateSlabCharge(
        List<TariffSlab> slabs,
        double totalKwh,
        List<SlabChargeBreakdown>? breakdowns = null)
    {
        if (slabs == null || slabs.Count == 0 || totalKwh <= 0)
            return 0m;

        decimal totalCharge = 0m;
        var sortedSlabs = slabs.OrderBy(s => s.FromKwh).ToList();

        foreach (var slab in sortedSlabs)
        {
            if (totalKwh <= slab.FromKwh)
                break;

            double slabMax = slab.ToKwh ?? double.MaxValue;
            double kwhInThisSlab = Math.Min(totalKwh, slabMax) - slab.FromKwh;

            if (kwhInThisSlab > 0)
            {
                decimal amount = Math.Round((decimal)kwhInThisSlab * slab.RatePerKwh, 2, MidpointRounding.AwayFromZero);
                totalCharge += amount;

                breakdowns?.Add(new SlabChargeBreakdown
                {
                    SlabIndex = slab.SlabIndex,
                    Label = slab.ToKwh.HasValue ? $"{slab.FromKwh:N0} - {slab.ToKwh.Value:N0} kWh" : $">{slab.FromKwh:N0} kWh",
                    KwhInSlab = Math.Round(kwhInThisSlab, 2),
                    RatePerKwh = slab.RatePerKwh,
                    Amount = amount
                });
            }
        }

        return Math.Round(totalCharge, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateTimeOfUseCharge(
        TariffPlan tariff,
        double totalKwh,
        IEnumerable<EnergyReading>? intervalReadings,
        List<TimeOfUseChargeBreakdown>? breakdowns = null)
    {
        var readings = intervalReadings?.ToList();
        if (readings == null || readings.Count == 0 || !readings.Any(r => r.Timestamp.Hour != 0 || readings.Count > 1))
        {
            // If only daily total is provided without hourly timestamps, calculate using flat rate or weighted rate
            decimal fallbackRate = tariff.FlatRatePerKwh > 0 ? tariff.FlatRatePerKwh : 0.15m;
            decimal charge = Math.Round((decimal)totalKwh * fallbackRate, 2, MidpointRounding.AwayFromZero);
            breakdowns?.Add(new TimeOfUseChargeBreakdown
            {
                BandName = "Standard / Daily Average (No interval data)",
                KwhInBand = Math.Round(totalKwh, 2),
                RatePerKwh = fallbackRate,
                Amount = charge
            });
            return charge;
        }

        // Aggregate readings by matching time band
        var bandTotals = new Dictionary<string, (double Kwh, decimal Rate)>();

        foreach (var reading in readings)
        {
            var matchedBand = tariff.TimeBands.FirstOrDefault(b => b.Matches(reading.Timestamp));
            string bandName = matchedBand?.Name ?? "Standard";
            decimal bandRate = matchedBand?.RatePerKwh ?? tariff.FlatRatePerKwh;

            if (!bandTotals.ContainsKey(bandName))
            {
                bandTotals[bandName] = (reading.ConsumptionKwh, bandRate);
            }
            else
            {
                var cur = bandTotals[bandName];
                bandTotals[bandName] = (cur.Kwh + reading.ConsumptionKwh, cur.Rate);
            }
        }

        decimal totalCharge = 0m;
        foreach (var kvp in bandTotals)
        {
            decimal bandAmount = Math.Round((decimal)kvp.Value.Kwh * kvp.Value.Rate, 2, MidpointRounding.AwayFromZero);
            totalCharge += bandAmount;

            breakdowns?.Add(new TimeOfUseChargeBreakdown
            {
                BandName = kvp.Key,
                KwhInBand = Math.Round(kvp.Value.Kwh, 2),
                RatePerKwh = kvp.Value.Rate,
                Amount = bandAmount
            });
        }

        return Math.Round(totalCharge, 2, MidpointRounding.AwayFromZero);
    }
}
