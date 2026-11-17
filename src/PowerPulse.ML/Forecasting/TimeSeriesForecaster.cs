using PowerPulse.Core.Enums;
using PowerPulse.Core.Models;
using PowerPulse.Core.Analytics;

namespace PowerPulse.ML.Forecasting;

public interface ITimeSeriesForecaster
{
    string ModelName { get; }
    bool IsMlModelActive { get; }
    ForecastResult ForecastCycleConsumption(
        IEnumerable<EnergyReading> historicalReadings,
        TariffPlan tariff,
        int billingCycleStartDay = 1,
        ForecastMethod preferredMethod = ForecastMethod.RecentSevenDays);
}

public class StatisticalForecaster : ITimeSeriesForecaster
{
    public string ModelName => "Adaptive Statistical Rolling Forecaster (WASM-Certified)";
    public bool IsMlModelActive => false; // Transparently labeled as statistical baseline

    public ForecastResult ForecastCycleConsumption(
        IEnumerable<EnergyReading> historicalReadings,
        TariffPlan tariff,
        int billingCycleStartDay = 1,
        ForecastMethod preferredMethod = ForecastMethod.RecentSevenDays)
    {
        return ForecastingEngine.GenerateForecast(
            historicalReadings,
            tariff,
            billingCycleStartDay,
            preferredMethod);
    }
}
