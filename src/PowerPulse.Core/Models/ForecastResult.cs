using PowerPulse.Core.Enums;

namespace PowerPulse.Core.Models;

public class ForecastResult
{
    public double ObservedKwh { get; set; }
    public int ObservedDays { get; set; }
    public int RemainingDays { get; set; }
    public double DailyAverageKwh { get; set; }
    public double Recent7DaysAverageKwh { get; set; }
    public double ExpectedRemainingKwh { get; set; }
    public double ProjectedTotalKwh { get; set; }
    public decimal ProjectedBill { get; set; }
    public ForecastMethod MethodUsed { get; set; } = ForecastMethod.RecentSevenDays;
    public double? HistoricalPreviousMonthTotalKwh { get; set; }
    public double? DifferenceVsPreviousMonthKwh => HistoricalPreviousMonthTotalKwh.HasValue
        ? ProjectedTotalKwh - HistoricalPreviousMonthTotalKwh.Value
        : null;
    public string? CoverageWarning { get; set; }
}
