using PowerPulse.Core.Models;

namespace PowerPulse.Infrastructure.Backup;

public class BackupDataModel
{
    public string AppVersion { get; set; } = "1.0.0";
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public HouseholdProfile? Profile { get; set; }
    public AppSettings? Settings { get; set; }
    public TariffPlan? Tariff { get; set; }
    public CarbonFactor? CarbonFactor { get; set; }
    public List<Appliance> Appliances { get; set; } = new();
    public List<EnergyDataset> Datasets { get; set; } = new();
    public List<EnergyReading> Readings { get; set; } = new();
    public List<Recommendation> Recommendations { get; set; } = new();
}
