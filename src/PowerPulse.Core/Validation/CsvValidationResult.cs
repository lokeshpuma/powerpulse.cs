using PowerPulse.Core.Enums;
using PowerPulse.Core.Models;

namespace PowerPulse.Core.Validation;

public class CsvColumnMapping
{
    public string TimestampColumn { get; set; } = string.Empty;
    public string? TimeColumn { get; set; }
    public string? ConsumptionColumn { get; set; }
    public string? CumulativeMeterColumn { get; set; }
    public string? UnitColumn { get; set; }
    public string? CostColumn { get; set; }
    public string? MeterIdColumn { get; set; }

    public EnergyUnit DefaultUnit { get; set; } = EnergyUnit.KWh;
    public ReadingType DetectedType { get; set; } = ReadingType.Interval;
}

public class CsvPreviewData
{
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public char DetectedDelimiter { get; set; } = ',';
    public int TotalEstimatedRows { get; set; }
    public long FileSizeBytes { get; set; }
}

public class CsvValidationResult
{
    public bool IsValid { get; set; } = true;
    public int TotalRowsParsed { get; set; }
    public int ValidRecordsCount { get; set; }
    public int DuplicateCount { get; set; }
    public int InvalidCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<EnergyReading> NormalizedReadings { get; set; } = new();
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public double TotalKwh { get; set; }
    public bool HasMeterResets { get; set; }
}
