using PowerPulse.Core.Validation;

namespace PowerPulse.Core.Interfaces;

public interface ICsvImportService
{
    Task<CsvPreviewData> DetectDelimiterAndPreviewAsync(Stream stream, string fileName, long fileSizeBytes, int previewRowCount = 20);
    CsvColumnMapping AutoDetectMapping(List<string> headers);
    Task<CsvValidationResult> ProcessAndValidateAsync(Stream stream, CsvColumnMapping mapping, string datasetId, char delimiter = ',');
}
