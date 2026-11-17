namespace PowerPulse.Core.Models;

public class CarbonFactor
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Region { get; set; } = "National Average (US eGRID)";
    public decimal KgCo2ePerKwh { get; set; } = 0.386m; // US avg approx ~0.386 kg/kWh
    public string SourceName { get; set; } = "US EPA eGRID";
    public string SourceUrl { get; set; } = "https://www.epa.gov/egrid";
    public int ReferenceYear { get; set; } = 2024;
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
    public bool IsConfigured { get; set; } = true;

    public static CarbonFactor CreateDefault()
    {
        return new CarbonFactor
        {
            Region = "US Average Grid",
            KgCo2ePerKwh = 0.386m,
            SourceName = "EPA eGRID 2024",
            SourceUrl = "https://www.epa.gov/egrid",
            ReferenceYear = 2024,
            IsConfigured = true
        };
    }
}
