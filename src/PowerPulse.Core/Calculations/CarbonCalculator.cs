using PowerPulse.Core.Models;

namespace PowerPulse.Core.Calculations;

public static class CarbonCalculator
{
    public static double? CalculateEmissionsKg(double measuredKwh, CarbonFactor? factor)
    {
        if (factor == null || !factor.IsConfigured || factor.KgCo2ePerKwh <= 0m)
            return null;

        return Math.Round(measuredKwh * (double)factor.KgCo2ePerKwh, 2);
    }

    public static double? CalculateApplianceEmissionsKg(Appliance appliance, int periodDays, CarbonFactor? factor)
    {
        if (factor == null || !factor.IsConfigured || factor.KgCo2ePerKwh <= 0m)
            return null;

        double kwh = ApplianceCalculator.CalculatePeriodKwh(appliance, periodDays);
        return Math.Round(kwh * (double)factor.KgCo2ePerKwh, 2);
    }
}
