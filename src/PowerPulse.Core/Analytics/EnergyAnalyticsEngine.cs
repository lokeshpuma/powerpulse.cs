using PowerPulse.Core.Enums;
using PowerPulse.Core.Models;
using PowerPulse.Core.Calculations;

namespace PowerPulse.Core.Analytics;

public static class EnergyAnalyticsEngine
{
    public static List<DailyEnergyAggregation> AggregateDaily(IEnumerable<EnergyReading> readings)
    {
        return readings
            .GroupBy(r => DateOnly.FromDateTime(r.Timestamp.LocalDateTime))
            .OrderBy(g => g.Key)
            .Select(g => new DailyEnergyAggregation
            {
                Date = g.Key,
                TotalKwh = Math.Round(g.Sum(r => r.ConsumptionKwh), 3),
                ReadingCount = g.Count(),
                MinHourlyKwh = Math.Round(g.Min(r => r.ConsumptionKwh), 3),
                MaxHourlyKwh = Math.Round(g.Max(r => r.ConsumptionKwh), 3),
                AvgHourlyKwh = Math.Round(g.Average(r => r.ConsumptionKwh), 3)
            })
            .ToList();
    }

    public static List<HourlyProfilePoint> CalculateHourlyProfile(IEnumerable<EnergyReading> readings)
    {
        var intervalReadings = readings.Where(r => r.Timestamp.Hour != 0 || readings.Count() > 30).ToList();
        var result = new List<HourlyProfilePoint>();

        for (int h = 0; h < 24; h++)
        {
            var hourList = intervalReadings.Where(r => r.Timestamp.Hour == h).ToList();
            if (hourList.Count > 0)
            {
                result.Add(new HourlyProfilePoint
                {
                    Hour = h,
                    AverageKwh = Math.Round(hourList.Average(r => r.ConsumptionKwh), 3),
                    MinKwh = Math.Round(hourList.Min(r => r.ConsumptionKwh), 3),
                    MaxKwh = Math.Round(hourList.Max(r => r.ConsumptionKwh), 3),
                    TotalReadings = hourList.Count
                });
            }
            else
            {
                result.Add(new HourlyProfilePoint
                {
                    Hour = h,
                    AverageKwh = 0,
                    MinKwh = 0,
                    MaxKwh = 0,
                    TotalReadings = 0
                });
            }
        }

        return result;
    }

    public static List<MonthlyAggregation> AggregateMonthly(IEnumerable<EnergyReading> readings, TariffPlan? tariff = null)
    {
        return readings
            .GroupBy(r => new { r.Timestamp.Year, r.Timestamp.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g =>
            {
                double totalKwh = Math.Round(g.Sum(r => r.ConsumptionKwh), 2);
                int distinctDays = g.Select(r => DateOnly.FromDateTime(r.Timestamp.LocalDateTime)).Distinct().Count();
                decimal cost = 0m;
                if (tariff != null)
                {
                    var bill = TariffCalculator.CalculateBill(tariff, totalKwh, g);
                    cost = bill.TotalEstimatedBill;
                }

                return new MonthlyAggregation
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    TotalKwh = totalKwh,
                    DaysCount = distinctDays,
                    AverageDailyKwh = distinctDays > 0 ? Math.Round(totalKwh / distinctDays, 2) : 0,
                    EstimatedCost = cost
                };
            })
            .ToList();
    }

    public static PeriodComparisonResult ComparePeriods(
        IEnumerable<EnergyReading> currentReadings,
        IEnumerable<EnergyReading> previousReadings,
        TariffPlan? tariff = null,
        CarbonFactor? carbonFactor = null)
    {
        var curList = currentReadings.ToList();
        var prevList = previousReadings.ToList();

        double curTotal = Math.Round(curList.Sum(r => r.ConsumptionKwh), 2);
        double prevTotal = Math.Round(prevList.Sum(r => r.ConsumptionKwh), 2);

        var curDaily = AggregateDaily(curList);
        var prevDaily = AggregateDaily(prevList);

        double curDailyAvg = curDaily.Count > 0 ? Math.Round(curDaily.Average(d => d.TotalKwh), 2) : 0;
        double prevDailyAvg = prevDaily.Count > 0 ? Math.Round(prevDaily.Average(d => d.TotalKwh), 2) : 0;

        var weekdays = curDaily.Where(d => !d.IsWeekend).ToList();
        var weekends = curDaily.Where(d => d.IsWeekend).ToList();

        double weekdayAvg = weekdays.Count > 0 ? Math.Round(weekdays.Average(d => d.TotalKwh), 2) : 0;
        double weekendAvg = weekends.Count > 0 ? Math.Round(weekends.Average(d => d.TotalKwh), 2) : 0;

        var hourly = CalculateHourlyProfile(curList);
        var peakHourObj = hourly.OrderByDescending(h => h.AverageKwh).FirstOrDefault();

        decimal? curCost = tariff != null ? TariffCalculator.CalculateBill(tariff, curTotal, curList).TotalEstimatedBill : null;
        decimal? prevCost = tariff != null ? TariffCalculator.CalculateBill(tariff, prevTotal, prevList).TotalEstimatedBill : null;

        double? curCarbon = CarbonCalculator.CalculateEmissionsKg(curTotal, carbonFactor);
        double? prevCarbon = CarbonCalculator.CalculateEmissionsKg(prevTotal, carbonFactor);

        return new PeriodComparisonResult
        {
            CurrentTotalKwh = curTotal,
            PreviousTotalKwh = prevTotal,
            CurrentDailyAverageKwh = curDailyAvg,
            PreviousDailyAverageKwh = prevDailyAvg,
            CurrentEstimatedCost = curCost,
            PreviousEstimatedCost = prevCost,
            CurrentCarbonKg = curCarbon,
            PreviousCarbonKg = prevCarbon,
            WeekdayAverageDailyKwh = weekdayAvg,
            WeekendAverageDailyKwh = weekendAvg,
            PeakUsageHour = peakHourObj?.Hour ?? 18,
            PeakUsageHourKwh = peakHourObj?.AverageKwh ?? 0
        };
    }

    public static List<HeatmapDataPoint> GenerateHeatmap(IEnumerable<EnergyReading> readings)
    {
        var result = new List<HeatmapDataPoint>();
        var readingList = readings.ToList();

        for (int day = 0; day < 7; day++)
        {
            var dayOfWeek = (DayOfWeek)day;
            for (int hour = 0; hour < 24; hour++)
            {
                var matching = readingList.Where(r => r.Timestamp.DayOfWeek == dayOfWeek && r.Timestamp.Hour == hour).ToList();
                double avg = matching.Count > 0 ? Math.Round(matching.Average(r => r.ConsumptionKwh), 3) : 0.0;

                result.Add(new HeatmapDataPoint
                {
                    DayOfWeek = dayOfWeek,
                    Hour = hour,
                    AverageKwh = avg
                });
            }
        }

        return result;
    }

    public static DateRangeFilter ResolveDateRange(DateRangeOption option, DateTimeOffset maxDate, int billingCycleStartDay = 1, DateTimeOffset? customStart = null, DateTimeOffset? customEnd = null)
    {
        DateTimeOffset end = maxDate;
        DateTimeOffset start;
        DateTimeOffset compStart;
        DateTimeOffset compEnd;

        switch (option)
        {
            case DateRangeOption.Last7Days:
                start = end.AddDays(-7);
                compEnd = start;
                compStart = compEnd.AddDays(-7);
                break;

            case DateRangeOption.Last30Days:
                start = end.AddDays(-30);
                compEnd = start;
                compStart = compEnd.AddDays(-30);
                break;

            case DateRangeOption.CurrentMonth:
                start = new DateTimeOffset(end.Year, end.Month, 1, 0, 0, 0, end.Offset);
                var prevMonthDate = start.AddMonths(-1);
                compStart = prevMonthDate;
                compEnd = start;
                break;

            case DateRangeOption.PreviousMonth:
                var pMonth = end.AddMonths(-1);
                start = new DateTimeOffset(pMonth.Year, pMonth.Month, 1, 0, 0, 0, end.Offset);
                end = new DateTimeOffset(end.Year, end.Month, 1, 0, 0, 0, end.Offset);
                var twoMonthsAgo = start.AddMonths(-1);
                compStart = twoMonthsAgo;
                compEnd = start;
                break;

            case DateRangeOption.CurrentBillingCycle:
                int day = Math.Clamp(billingCycleStartDay, 1, 28);
                DateTime cycleStartDate;
                if (end.Day >= day)
                {
                    cycleStartDate = new DateTime(end.Year, end.Month, day);
                }
                else
                {
                    var prev = end.AddMonths(-1);
                    cycleStartDate = new DateTime(prev.Year, prev.Month, day);
                }
                start = new DateTimeOffset(cycleStartDate, end.Offset);
                var prevCycleStart = start.AddMonths(-1);
                compStart = prevCycleStart;
                compEnd = start;
                break;

            case DateRangeOption.Custom:
                start = customStart ?? end.AddDays(-30);
                end = customEnd ?? maxDate;
                var duration = end - start;
                compEnd = start;
                compStart = compEnd - duration;
                break;

            default:
                start = end.AddDays(-30);
                compEnd = start;
                compStart = compEnd.AddDays(-30);
                break;
        }

        return new DateRangeFilter
        {
            Option = option,
            StartDate = start,
            EndDate = end,
            ComparisonStartDate = compStart,
            ComparisonEndDate = compEnd
        };
    }
}
