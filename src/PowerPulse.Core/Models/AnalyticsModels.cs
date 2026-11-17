using PowerPulse.Core.Enums;

namespace PowerPulse.Core.Models;

public class DailyEnergyAggregation
{
    public DateOnly Date { get; set; }
    public double TotalKwh { get; set; }
    public int ReadingCount { get; set; }
    public double MinHourlyKwh { get; set; }
    public double MaxHourlyKwh { get; set; }
    public double AvgHourlyKwh { get; set; }
    public bool IsWeekend => Date.DayOfWeek == DayOfWeek.Saturday || Date.DayOfWeek == DayOfWeek.Sunday;
}

public class HourlyProfilePoint
{
    public int Hour { get; set; } // 0-23
    public double AverageKwh { get; set; }
    public double MinKwh { get; set; }
    public double MaxKwh { get; set; }
    public int TotalReadings { get; set; }
}

public class MonthlyAggregation
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public double TotalKwh { get; set; }
    public double AverageDailyKwh { get; set; }
    public int DaysCount { get; set; }
    public decimal EstimatedCost { get; set; }
}

public class DateRangeFilter
{
    public DateRangeOption Option { get; set; } = DateRangeOption.Last30Days;
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public DateTimeOffset ComparisonStartDate { get; set; }
    public DateTimeOffset ComparisonEndDate { get; set; }
}

public class PeriodComparisonResult
{
    public double CurrentTotalKwh { get; set; }
    public double PreviousTotalKwh { get; set; }
    public double AbsoluteDifferenceKwh => CurrentTotalKwh - PreviousTotalKwh;
    public double? PercentageChange => PreviousTotalKwh > 0.0001
        ? ((CurrentTotalKwh - PreviousTotalKwh) / PreviousTotalKwh) * 100.0
        : null;

    public decimal? CurrentEstimatedCost { get; set; }
    public decimal? PreviousEstimatedCost { get; set; }
    public decimal? CostDifference => CurrentEstimatedCost.HasValue && PreviousEstimatedCost.HasValue
        ? CurrentEstimatedCost.Value - PreviousEstimatedCost.Value
        : null;

    public double? CurrentCarbonKg { get; set; }
    public double? PreviousCarbonKg { get; set; }
    public double? CarbonDifference => CurrentCarbonKg.HasValue && PreviousCarbonKg.HasValue
        ? CurrentCarbonKg.Value - PreviousCarbonKg.Value
        : null;

    public double CurrentDailyAverageKwh { get; set; }
    public double PreviousDailyAverageKwh { get; set; }
    public double PeakUsageHourKwh { get; set; }
    public int PeakUsageHour { get; set; }
    public double WeekdayAverageDailyKwh { get; set; }
    public double WeekendAverageDailyKwh { get; set; }
}

public class HeatmapDataPoint
{
    public DayOfWeek DayOfWeek { get; set; }
    public int Hour { get; set; }
    public double AverageKwh { get; set; }
}

public class SpendBreakdownItem
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public double Percentage { get; set; }
    public string ColorHex { get; set; } = "#1DA058";
}
