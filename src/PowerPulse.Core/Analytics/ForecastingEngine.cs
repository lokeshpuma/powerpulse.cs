using PowerPulse.Core.Enums;
using PowerPulse.Core.Models;
using PowerPulse.Core.Calculations;

namespace PowerPulse.Core.Analytics;

public static class ForecastingEngine
{
    public static ForecastResult GenerateForecast(
        IEnumerable<EnergyReading> readings,
        TariffPlan tariff,
        int billingCycleStartDay = 1,
        ForecastMethod method = ForecastMethod.RecentSevenDays,
        DateTimeOffset? asOfDate = null)
    {
        var readingList = readings.ToList();
        var dailyData = EnergyAnalyticsEngine.AggregateDaily(readingList);

        DateTimeOffset now = asOfDate ?? (readingList.Count > 0 ? readingList.Max(r => r.Timestamp) : DateTimeOffset.UtcNow);

        // Determine current billing cycle start and end dates
        int cycleDay = Math.Clamp(billingCycleStartDay, 1, 28);
        DateTime cycleStart;
        if (now.Day >= cycleDay)
        {
            cycleStart = new DateTime(now.Year, now.Month, cycleDay);
        }
        else
        {
            var prev = now.AddMonths(-1);
            cycleStart = new DateTime(prev.Year, prev.Month, cycleDay);
        }

        DateTime cycleEnd = cycleStart.AddMonths(1);
        int totalCycleDays = (int)(cycleEnd - cycleStart).TotalDays;

        DateOnly cycleStartDateOnly = DateOnly.FromDateTime(cycleStart);
        DateOnly cycleEndDateOnly = DateOnly.FromDateTime(cycleEnd);

        var cycleDaily = dailyData
            .Where(d => d.Date >= cycleStartDateOnly && d.Date < cycleEndDateOnly)
            .OrderBy(d => d.Date)
            .ToList();

        int observedDays = cycleDaily.Count;
        int remainingDays = Math.Max(0, totalCycleDays - observedDays);

        double observedKwh = Math.Round(cycleDaily.Sum(d => d.TotalKwh), 2);
        double allDailyAvg = observedDays > 0 ? Math.Round(observedKwh / observedDays, 2) : 0.0;

        // Recent 7 days average
        var recent7 = dailyData.OrderByDescending(d => d.Date).Take(7).ToList();
        double recent7Avg = recent7.Count > 0 ? Math.Round(recent7.Average(d => d.TotalKwh), 2) : allDailyAvg;

        double projectedDailyUsage = method == ForecastMethod.RecentSevenDays ? recent7Avg : allDailyAvg;
        double expectedRemainingKwh = Math.Round(projectedDailyUsage * remainingDays, 2);
        double projectedTotalKwh = Math.Round(observedKwh + expectedRemainingKwh, 2);

        // Calculate bill projection
        var billEstimate = TariffCalculator.CalculateBill(
            tariff,
            projectedTotalKwh,
            null,
            null,
            new DateTimeOffset(cycleStart, now.Offset),
            new DateTimeOffset(cycleEnd, now.Offset));

        // Historical previous cycle comparison
        DateTime prevCycleStart = cycleStart.AddMonths(-1);
        DateOnly prevCycleStartDateOnly = DateOnly.FromDateTime(prevCycleStart);
        var prevCycleDaily = dailyData
            .Where(d => d.Date >= prevCycleStartDateOnly && d.Date < cycleStartDateOnly)
            .ToList();
        double? prevTotalKwh = prevCycleDaily.Count > 0 ? Math.Round(prevCycleDaily.Sum(d => d.TotalKwh), 2) : null;

        string? coverageWarning = null;
        if (observedDays < 3)
        {
            coverageWarning = "Less than 3 days of readings in current cycle. Forecast confidence is low.";
        }
        else if (observedDays < 7)
        {
            coverageWarning = "Less than a full week observed in current cycle. Projections may vary as weekly usage patterns stabilize.";
        }

        return new ForecastResult
        {
            ObservedKwh = observedKwh,
            ObservedDays = observedDays,
            RemainingDays = remainingDays,
            DailyAverageKwh = allDailyAvg,
            Recent7DaysAverageKwh = recent7Avg,
            ExpectedRemainingKwh = expectedRemainingKwh,
            ProjectedTotalKwh = projectedTotalKwh,
            ProjectedBill = billEstimate.TotalEstimatedBill,
            MethodUsed = method,
            HistoricalPreviousMonthTotalKwh = prevTotalKwh,
            CoverageWarning = coverageWarning
        };
    }
}
