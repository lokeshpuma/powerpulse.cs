using System.Text;
using PowerPulse.Core.Enums;
using PowerPulse.Core.Validation;
using PowerPulse.Infrastructure.Csv;
using Xunit;

namespace PowerPulse.Infrastructure.Tests;

public class CsvImportTests
{
    private readonly CsvImportService _importService = new();

    [Fact]
    public async Task ProcessAndValidate_ValidIntervalCsv_ParsesNormalizedReadings()
    {
        string csv = @"timestamp,consumption_kwh
2026-10-01T00:00:00+00:00,0.45
2026-10-01T01:00:00+00:00,0.38
2026-10-01T02:00:00+00:00,0.42";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var mapping = new CsvColumnMapping
        {
            TimestampColumn = "timestamp",
            ConsumptionColumn = "consumption_kwh",
            DefaultUnit = EnergyUnit.KWh,
            DetectedType = ReadingType.Interval
        };

        var result = await _importService.ProcessAndValidateAsync(stream, mapping, "dataset-1", ',');

        Assert.True(result.IsValid);
        Assert.Equal(3, result.ValidRecordsCount);
        Assert.Equal(0, result.InvalidCount);
        Assert.Equal(1.25, result.TotalKwh, precision: 2);
    }

    [Fact]
    public async Task ProcessAndValidate_WhUnitConversion_NormalizesToKWh()
    {
        string csv = @"timestamp,consumption_wh
2026-10-01T00:00:00+00:00,1500
2026-10-01T01:00:00+00:00,2500";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var mapping = new CsvColumnMapping
        {
            TimestampColumn = "timestamp",
            ConsumptionColumn = "consumption_wh",
            DefaultUnit = EnergyUnit.Wh,
            DetectedType = ReadingType.Interval
        };

        var result = await _importService.ProcessAndValidateAsync(stream, mapping, "dataset-1", ',');

        Assert.True(result.IsValid);
        Assert.Equal(2, result.ValidRecordsCount);
        // 1500 Wh = 1.5 kWh, 2500 Wh = 2.5 kWh -> Total = 4.0 kWh
        Assert.Equal(4.0, result.TotalKwh, precision: 2);
    }

    [Fact]
    public async Task ProcessAndValidate_CumulativeMeterReadings_CalculatesDeltas()
    {
        string csv = @"timestamp,meter_reading_kwh
2026-10-01,1000.0
2026-10-02,1010.5
2026-10-03,1022.0";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var mapping = new CsvColumnMapping
        {
            TimestampColumn = "timestamp",
            CumulativeMeterColumn = "meter_reading_kwh",
            DefaultUnit = EnergyUnit.KWh,
            DetectedType = ReadingType.Cumulative
        };

        var result = await _importService.ProcessAndValidateAsync(stream, mapping, "dataset-1", ',');

        Assert.True(result.IsValid);
        Assert.Equal(3, result.ValidRecordsCount);

        // First reading baseline consumption = 0.0
        // Second delta = 1010.5 - 1000.0 = 10.5 kWh
        // Third delta = 1022.0 - 1010.5 = 11.5 kWh
        // Total = 22.0 kWh
        Assert.Equal(22.0, result.TotalKwh, precision: 1);
        Assert.Equal(10.5, result.NormalizedReadings[1].ConsumptionKwh, precision: 1);
        Assert.Equal(11.5, result.NormalizedReadings[2].ConsumptionKwh, precision: 1);
    }

    [Fact]
    public async Task ProcessAndValidate_NegativeConsumption_SkipsInvalidRow()
    {
        string csv = @"timestamp,consumption_kwh
2026-10-01T00:00:00+00:00,0.50
2026-10-01T01:00:00+00:00,-2.30
2026-10-01T02:00:00+00:00,0.40";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var mapping = new CsvColumnMapping
        {
            TimestampColumn = "timestamp",
            ConsumptionColumn = "consumption_kwh"
        };

        var result = await _importService.ProcessAndValidateAsync(stream, mapping, "dataset-1", ',');

        Assert.True(result.IsValid);
        Assert.Equal(2, result.ValidRecordsCount);
        Assert.Equal(1, result.InvalidCount);
        Assert.Contains(result.Warnings, w => w.Contains("Negative"));
    }

    [Fact]
    public async Task ProcessAndValidate_DuplicateTimestamps_Deduplicates()
    {
        string csv = @"timestamp,consumption_kwh
2026-10-01T00:00:00+00:00,0.50
2026-10-01T00:00:00+00:00,0.80
2026-10-01T01:00:00+00:00,0.60";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var mapping = new CsvColumnMapping
        {
            TimestampColumn = "timestamp",
            ConsumptionColumn = "consumption_kwh"
        };

        var result = await _importService.ProcessAndValidateAsync(stream, mapping, "dataset-1", ',');

        Assert.True(result.IsValid);
        Assert.Equal(2, result.ValidRecordsCount);
        Assert.Equal(1, result.DuplicateCount);
    }

    [Fact]
    public async Task ProcessAndValidate_CumulativeMeterReset_FlagsWarning()
    {
        string csv = @"timestamp,meter_reading_kwh
2026-10-01,9990.0
2026-10-02,0010.0"; // Meter reset / rollover

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var mapping = new CsvColumnMapping
        {
            TimestampColumn = "timestamp",
            CumulativeMeterColumn = "meter_reading_kwh"
        };

        var result = await _importService.ProcessAndValidateAsync(stream, mapping, "dataset-1", ',');

        Assert.True(result.IsValid);
        Assert.True(result.HasMeterResets);
        Assert.Contains(result.Warnings, w => w.Contains("reset/rollover"));
    }
}
