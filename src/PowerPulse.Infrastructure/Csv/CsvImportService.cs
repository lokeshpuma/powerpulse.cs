using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using PowerPulse.Core.Calculations;
using PowerPulse.Core.Enums;
using PowerPulse.Core.Interfaces;
using PowerPulse.Core.Models;
using PowerPulse.Core.Validation;

namespace PowerPulse.Infrastructure.Csv;

public class CsvImportService : ICsvImportService
{
    private const int MaxAllowedRecords = 100000;
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public async Task<CsvPreviewData> DetectDelimiterAndPreviewAsync(
        Stream stream, string fileName, long fileSizeBytes, int previewRowCount = 20)
    {
        if (fileSizeBytes > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"File size ({fileSizeBytes / (1024 * 1024)} MB) exceeds maximum allowed limit of {MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        // Read first few lines to detect delimiter
        using var reader = new StreamReader(memoryStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var sampleLines = new List<string>();
        for (int i = 0; i < 15; i++)
        {
            var line = await reader.ReadLineAsync();
            if (line == null)
                break;
            if (!string.IsNullOrWhiteSpace(line))
            {
                sampleLines.Add(line);
            }
        }

        char detectedDelimiter = DetectDelimiter(sampleLines);

        // Reset and parse with CsvHelper
        memoryStream.Position = 0;
        using var csvReader = new StreamReader(memoryStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = detectedDelimiter.ToString(),
            BadDataFound = null,
            MissingFieldFound = null,
            HeaderValidated = null,
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(csvReader, csvConfig);
        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord?.Select(h => h.Trim()).ToList() ?? new List<string>();

        var rows = new List<List<string>>();
        int rowCount = 0;
        while (await csv.ReadAsync())
        {
            rowCount++;
            if (rows.Count < previewRowCount)
            {
                var rowData = new List<string>();
                for (int i = 0; i < headers.Count; i++)
                {
                    rowData.Add(csv.GetField(i)?.Trim() ?? string.Empty);
                }
                rows.Add(rowData);
            }
        }

        return new CsvPreviewData
        {
            Headers = headers,
            Rows = rows,
            DetectedDelimiter = detectedDelimiter,
            TotalEstimatedRows = rowCount,
            FileSizeBytes = fileSizeBytes
        };
    }

    public CsvColumnMapping AutoDetectMapping(List<string> headers)
    {
        var mapping = new CsvColumnMapping();

        foreach (var header in headers)
        {
            var clean = header.Trim().ToLowerInvariant().Replace("_", "").Replace(" ", "").Replace("-", "");

            if (string.IsNullOrEmpty(mapping.TimestampColumn) &&
                (clean.Contains("timestamp") || clean.Contains("datetime") || clean.Equals("date") || clean.Contains("time")))
            {
                mapping.TimestampColumn = header;
            }
            else if (string.IsNullOrEmpty(mapping.TimeColumn) && clean.Equals("time"))
            {
                mapping.TimeColumn = header;
            }
            else if (string.IsNullOrEmpty(mapping.CumulativeMeterColumn) &&
                (clean.Contains("meterreading") || clean.Contains("cumulative") || clean.Contains("totalreading") || clean.Contains("meterkwh")))
            {
                mapping.CumulativeMeterColumn = header;
                mapping.DetectedType = ReadingType.Cumulative;
            }
            else if (string.IsNullOrEmpty(mapping.ConsumptionColumn) &&
                (clean.Contains("kwh") || clean.Contains("consumption") || clean.Contains("energy") || clean.Contains("usage") || clean.Contains("power")))
            {
                mapping.ConsumptionColumn = header;
                mapping.DetectedType = ReadingType.Interval;
            }
            else if (string.IsNullOrEmpty(mapping.UnitColumn) && clean.Contains("unit"))
            {
                mapping.UnitColumn = header;
            }
            else if (string.IsNullOrEmpty(mapping.CostColumn) && (clean.Contains("cost") || clean.Contains("price") || clean.Contains("bill") || clean.Contains("amount")))
            {
                mapping.CostColumn = header;
            }
            else if (string.IsNullOrEmpty(mapping.MeterIdColumn) && (clean.Contains("meterid") || clean.Contains("meter") || clean.Contains("deviceid")))
            {
                mapping.MeterIdColumn = header;
            }
        }

        return mapping;
    }

    public async Task<CsvValidationResult> ProcessAndValidateAsync(
        Stream stream, CsvColumnMapping mapping, string datasetId, char delimiter = ',')
    {
        var result = new CsvValidationResult();

        if (string.IsNullOrWhiteSpace(mapping.TimestampColumn))
        {
            result.IsValid = false;
            result.Errors.Add("Timestamp/Date column must be specified.");
            return result;
        }

        bool hasConsumption = !string.IsNullOrWhiteSpace(mapping.ConsumptionColumn);
        bool hasCumulative = !string.IsNullOrWhiteSpace(mapping.CumulativeMeterColumn);

        if (!hasConsumption && !hasCumulative)
        {
            result.IsValid = false;
            result.Errors.Add("Either an interval consumption column or a cumulative meter reading column must be mapped.");
            return result;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter.ToString(),
            BadDataFound = context =>
            {
                result.Warnings.Add($"Bad data detected in field: {context.Field}");
            },
            MissingFieldFound = null,
            HeaderValidated = null,
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(reader, config);
        await csv.ReadAsync();
        csv.ReadHeader();

        var parsedItems = new List<RawReadingItem>();
        int rowNumber = 1;

        while (await csv.ReadAsync())
        {
            rowNumber++;
            result.TotalRowsParsed++;

            if (result.TotalRowsParsed > MaxAllowedRecords)
            {
                result.Errors.Add($"Dataset exceeds the maximum limit of {MaxAllowedRecords} records. Processing aborted.");
                result.IsValid = false;
                return result;
            }

            string? timestampRaw = csv.GetField(mapping.TimestampColumn);
            if (string.IsNullOrWhiteSpace(timestampRaw))
            {
                result.InvalidCount++;
                result.Warnings.Add($"Row {rowNumber}: Missing timestamp. Row skipped.");
                continue;
            }

            // If separate time column is mapped
            if (!string.IsNullOrWhiteSpace(mapping.TimeColumn))
            {
                string? timeRaw = csv.GetField(mapping.TimeColumn);
                if (!string.IsNullOrWhiteSpace(timeRaw))
                {
                    timestampRaw = $"{timestampRaw} {timeRaw}";
                }
            }

            if (!TryParseTimestamp(timestampRaw, out DateTimeOffset timestamp))
            {
                result.InvalidCount++;
                result.Warnings.Add($"Row {rowNumber}: Unsupported or ambiguous timestamp format '{timestampRaw}'. Row skipped.");
                continue;
            }

            // Unit resolution
            EnergyUnit unit = mapping.DefaultUnit;
            if (!string.IsNullOrWhiteSpace(mapping.UnitColumn))
            {
                string? unitVal = csv.GetField(mapping.UnitColumn);
                if (UnitConverter.TryParseUnit(unitVal, out var parsedUnit))
                {
                    unit = parsedUnit;
                }
                else if (!string.IsNullOrWhiteSpace(unitVal))
                {
                    result.InvalidCount++;
                    result.Warnings.Add($"Row {rowNumber}: Unrecognized energy unit '{unitVal}'. Row skipped.");
                    continue;
                }
            }

            string? meterId = !string.IsNullOrWhiteSpace(mapping.MeterIdColumn)
                ? csv.GetField(mapping.MeterIdColumn)
                : null;

            decimal? cost = null;
            if (!string.IsNullOrWhiteSpace(mapping.CostColumn))
            {
                string? costStr = csv.GetField(mapping.CostColumn);
                if (decimal.TryParse(costStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedCost))
                {
                    cost = parsedCost;
                }
            }

            if (hasCumulative)
            {
                string? cumRaw = csv.GetField(mapping.CumulativeMeterColumn!);
                if (!double.TryParse(cumRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out double cumVal))
                {
                    result.InvalidCount++;
                    result.Warnings.Add($"Row {rowNumber}: Invalid numeric cumulative reading '{cumRaw}'. Row skipped.");
                    continue;
                }

                double cumKwh = UnitConverter.ToKWh(cumVal, unit);
                parsedItems.Add(new RawReadingItem
                {
                    RowNumber = rowNumber,
                    Timestamp = timestamp,
                    IsCumulative = true,
                    CumulativeReading = cumKwh,
                    MeterId = meterId,
                    Cost = cost
                });
            }
            else
            {
                string? consRaw = csv.GetField(mapping.ConsumptionColumn!);
                if (!double.TryParse(consRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out double consVal))
                {
                    result.InvalidCount++;
                    result.Warnings.Add($"Row {rowNumber}: Invalid numeric consumption value '{consRaw}'. Row skipped.");
                    continue;
                }

                if (consVal < 0)
                {
                    result.InvalidCount++;
                    result.Warnings.Add($"Row {rowNumber}: Negative consumption value ({consVal}) is not permitted. Row skipped.");
                    continue;
                }

                double consKwh = UnitConverter.ToKWh(consVal, unit);
                parsedItems.Add(new RawReadingItem
                {
                    RowNumber = rowNumber,
                    Timestamp = timestamp,
                    IsCumulative = false,
                    Consumption = consKwh,
                    MeterId = meterId,
                    Cost = cost
                });
            }
        }

        // Process parsed items: deduplication, sorting, delta calculations
        if (parsedItems.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add("No valid records found in the uploaded CSV file.");
            return result;
        }

        // Group by MeterId to handle multi-meter data
        var meterGroups = parsedItems.GroupBy(p => p.MeterId ?? string.Empty);
        var finalReadings = new List<EnergyReading>();

        foreach (var group in meterGroups)
        {
            var sortedList = group.OrderBy(p => p.Timestamp).ToList();

            // Deduplicate exact timestamps
            var uniqueList = new List<RawReadingItem>();
            foreach (var item in sortedList)
            {
                if (uniqueList.Count > 0 && uniqueList[^1].Timestamp == item.Timestamp)
                {
                    result.DuplicateCount++;
                    result.Warnings.Add($"Row {item.RowNumber}: Duplicate timestamp {item.Timestamp:O} detected. Earlier record preserved.");
                    continue;
                }
                uniqueList.Add(item);
            }

            if (hasCumulative)
            {
                // Calculate consumption as difference between successive readings
                for (int i = 0; i < uniqueList.Count; i++)
                {
                    var cur = uniqueList[i];
                    double consumption = 0.0;

                    if (i > 0)
                    {
                        var prev = uniqueList[i - 1];
                        double delta = cur.CumulativeReading - prev.CumulativeReading;

                        if (delta < 0)
                        {
                            result.HasMeterResets = true;
                            result.Warnings.Add($"Row {cur.RowNumber}: Cumulative meter reading decreased from {prev.CumulativeReading:N1} to {cur.CumulativeReading:N1}. Flagged as potential meter reset/rollover.");
                            consumption = 0.0; // Do not silently forge negative readings
                        }
                        else
                        {
                            consumption = Math.Round(delta, 4);
                        }
                    }

                    finalReadings.Add(new EnergyReading
                    {
                        DatasetId = datasetId,
                        Timestamp = cur.Timestamp,
                        ConsumptionKwh = consumption,
                        IsCumulativeReading = true,
                        MeterReadingKwh = cur.CumulativeReading,
                        MeterId = cur.MeterId,
                        Cost = cur.Cost
                    });
                }
            }
            else
            {
                foreach (var cur in uniqueList)
                {
                    finalReadings.Add(new EnergyReading
                    {
                        DatasetId = datasetId,
                        Timestamp = cur.Timestamp,
                        ConsumptionKwh = Math.Round(cur.Consumption, 4),
                        IsCumulativeReading = false,
                        MeterId = cur.MeterId,
                        Cost = cur.Cost
                    });
                }
            }
        }

        result.NormalizedReadings = finalReadings.OrderBy(r => r.Timestamp).ToList();
        result.ValidRecordsCount = result.NormalizedReadings.Count;

        if (result.ValidRecordsCount > 0)
        {
            result.StartDate = result.NormalizedReadings.First().Timestamp;
            result.EndDate = result.NormalizedReadings.Last().Timestamp;
            result.TotalKwh = Math.Round(result.NormalizedReadings.Sum(r => r.ConsumptionKwh), 2);
        }

        result.IsValid = result.ValidRecordsCount > 0 && result.Errors.Count == 0;
        return result;
    }

    private static char DetectDelimiter(List<string> sampleLines)
    {
        if (sampleLines == null || sampleLines.Count == 0)
            return ',';

        var commaCount = sampleLines.Sum(l => l.Count(c => c == ','));
        var semicolonCount = sampleLines.Sum(l => l.Count(c => c == ';'));
        var tabCount = sampleLines.Sum(l => l.Count(c => c == '\t'));

        if (semicolonCount > commaCount && semicolonCount > tabCount)
            return ';';
        if (tabCount > commaCount && tabCount > semicolonCount)
            return '\t';

        return ',';
    }

    private static bool TryParseTimestamp(string raw, out DateTimeOffset result)
    {
        // Try ISO 8601, round-trip, common energy export formats
        string[] formats =
        {
            "yyyy-MM-ddTHH:mm:sszzz",
            "yyyy-MM-ddTHH:mm:ssK",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd",
            "yyyy/MM/dd HH:mm:ss",
            "yyyy/MM/dd",
            "dd-MM-yyyy HH:mm:ss",
            "dd-MM-yyyy HH:mm",
            "dd-MM-yyyy",
            "dd/MM/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm",
            "dd/MM/yyyy",
            "MM/dd/yyyy HH:mm:ss",
            "MM/dd/yyyy HH:mm",
            "MM/dd/yyyy",
            "yyyyMMddHHmmss",
            "yyyyMMdd"
        };

        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            return true;

        if (DateTimeOffset.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out result))
            return true;

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            result = new DateTimeOffset(dt, TimeSpan.Zero);
            return true;
        }

        result = default;
        return false;
    }

    private class RawReadingItem
    {
        public int RowNumber { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public bool IsCumulative { get; set; }
        public double Consumption { get; set; }
        public double CumulativeReading { get; set; }
        public string? MeterId { get; set; }
        public decimal? Cost { get; set; }
    }
}
