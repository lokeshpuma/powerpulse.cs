namespace PowerPulse.Core.Models;

public class AnomalyResult
{
    public DateOnly Date { get; set; }
    public double ConsumptionKwh { get; set; }
    public double RollingMedianKwh { get; set; }
    public double DifferenceKwh { get; set; }
    public double PercentageDifference { get; set; }
    public double RobustZScore { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string DetectionMethod { get; set; } = "Rolling MAD (Median Absolute Deviation)";
    public bool IsAnomaly { get; set; }
}
