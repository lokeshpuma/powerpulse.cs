using PowerPulse.Core.Models;

namespace PowerPulse.Core.Calculations;

public static class ApplianceCalculator
{
    public static double CalculateDailyKwh(Appliance appliance)
    {
        if (!appliance.IncludeInCalculation)
            return 0.0;

        double dutyCycle = appliance.DutyCycle <= 0.0 ? 1.0 : appliance.DutyCycle;
        double activeKwh = (appliance.RatedWatts * appliance.Quantity * appliance.HoursPerDay * dutyCycle) / 1000.0;
        double standbyKwh = (appliance.StandbyWatts * appliance.Quantity * appliance.StandbyHoursPerDay) / 1000.0;

        // Multiply by (days per week / 7) for daily average
        double weeklyFactor = Math.Clamp(appliance.UsageDaysPerWeek, 0.0, 7.0) / 7.0;
        return (activeKwh * weeklyFactor) + standbyKwh;
    }

    public static double CalculatePeriodKwh(Appliance appliance, int periodDays)
    {
        if (!appliance.IncludeInCalculation || periodDays <= 0)
            return 0.0;

        double dutyCycle = appliance.DutyCycle <= 0.0 ? 1.0 : appliance.DutyCycle;
        double activeDays = periodDays * Math.Clamp(appliance.UsageDaysPerWeek, 0.0, 7.0) / 7.0;
        double activeEnergy = (appliance.RatedWatts * appliance.Quantity * appliance.HoursPerDay * activeDays * dutyCycle) / 1000.0;
        double standbyEnergy = (appliance.StandbyWatts * appliance.Quantity * appliance.StandbyHoursPerDay * periodDays) / 1000.0;

        return activeEnergy + standbyEnergy;
    }

    public static double CalculateTotalDailyKwh(IEnumerable<Appliance> appliances)
    {
        return appliances.Where(a => a.IncludeInCalculation).Sum(CalculateDailyKwh);
    }

    public static double CalculateTotalPeriodKwh(IEnumerable<Appliance> appliances, int periodDays)
    {
        return appliances.Where(a => a.IncludeInCalculation).Sum(a => CalculatePeriodKwh(a, periodDays));
    }

    public static decimal EstimateAppliancePeriodCost(Appliance appliance, int periodDays, decimal effectiveRatePerKwh)
    {
        double kwh = CalculatePeriodKwh(appliance, periodDays);
        return Math.Round((decimal)kwh * effectiveRatePerKwh, 2, MidpointRounding.AwayFromZero);
    }

    public static (double MeasuredKwh, double EstimatedApplianceKwh, double DifferenceKwh, bool IsOverestimated) CompareWithMeter(
        double totalMeterKwh, IEnumerable<Appliance> appliances, int periodDays)
    {
        double estimatedKwh = CalculateTotalPeriodKwh(appliances, periodDays);
        double diff = totalMeterKwh - estimatedKwh;
        bool isOverestimated = estimatedKwh > totalMeterKwh;
        return (totalMeterKwh, estimatedKwh, diff, isOverestimated);
    }
}
