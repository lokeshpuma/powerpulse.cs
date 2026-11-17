using PowerPulse.Core.Enums;

namespace PowerPulse.Core.Models;

public class EnergyDataset
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public DateTime ImportDate { get; set; } = DateTime.UtcNow;
    public int RecordCount { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public ImportStatus Status { get; set; } = ImportStatus.Success;
    public List<string> ValidationWarnings { get; set; } = new();
    public bool IsSampleData { get; set; } = false;
}
