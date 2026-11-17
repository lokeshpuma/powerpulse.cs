namespace PowerPulse.Core.Models;

public class EnergyReading
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DatasetId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public double ConsumptionKwh { get; set; }
    public bool IsCumulativeReading { get; set; } = false;
    public double? MeterReadingKwh { get; set; }
    public string? MeterId { get; set; }
    public decimal? Cost { get; set; }
}
