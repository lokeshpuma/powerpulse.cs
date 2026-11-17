using PowerPulse.Core.Enums;

namespace PowerPulse.Core.Models;

public class AppSettings
{
    public string Theme { get; set; } = "light";
    public DateRangeOption DefaultDateRange { get; set; } = DateRangeOption.Last30Days;
    public int MaxFileSizeMb { get; set; } = 10;
    public int MaxRecords { get; set; } = 100000;
    public double AnomalySensitivityThreshold { get; set; } = 3.5;
    public ForecastMethod DefaultForecastMethod { get; set; } = ForecastMethod.RecentSevenDays;
}
