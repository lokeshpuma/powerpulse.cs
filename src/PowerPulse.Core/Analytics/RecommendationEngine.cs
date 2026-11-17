using PowerPulse.Core.Enums;
using PowerPulse.Core.Models;
using PowerPulse.Core.Calculations;

namespace PowerPulse.Core.Analytics;

public static class RecommendationEngine
{
    public static List<Recommendation> GenerateRecommendations(
        List<EnergyReading> readings,
        List<Appliance> appliances,
        TariffPlan tariff,
        DateRangeFilter dateRange)
    {
        var recommendations = new List<Recommendation>();

        if (readings == null || readings.Count == 0)
            return recommendations;

        var daily = EnergyAnalyticsEngine.AggregateDaily(readings);
        double totalKwh = daily.Sum(d => d.TotalKwh);
        int periodDays = Math.Max(1, daily.Count);

        // 1. Check for Anomaly Spikes
        var anomalies = AnomalyDetectionEngine.DetectAnomalies(daily, 14, 3.5);
        var recentSpike = anomalies.LastOrDefault(a => a.IsAnomaly && a.DifferenceKwh > 0);
        if (recentSpike != null)
        {
            recommendations.Add(new Recommendation
            {
                Type = RecommendationType.AnomalySpike,
                Title = "Unusual Consumption Spike Detected",
                Description = $"Daily electricity usage on {recentSpike.Date:MMM dd} was significantly higher than your baseline.",
                Evidence = $"Measured: {recentSpike.ConsumptionKwh:N1} kWh vs Rolling Baseline: {recentSpike.RollingMedianKwh:N1} kWh (+{recentSpike.PercentageDifference:N0}% increase, Robust Z-Score: {recentSpike.RobustZScore:N1}).",
                Calculation = $"Baseline MAD threshold: 3.5. Deviation was +{recentSpike.DifferenceKwh:N1} kWh over median.",
                PracticalSuggestion = "Inspect whether heavy loads (space heaters, air conditioning, oven, or power tools) were left running or operated concurrently during this day.",
                Priority = 1
            });
        }

        // 2. Rising Energy Usage Check
        var prevRangeReadings = readings.Where(r => r.Timestamp >= dateRange.ComparisonStartDate && r.Timestamp < dateRange.ComparisonEndDate).ToList();
        var curRangeReadings = readings.Where(r => r.Timestamp >= dateRange.StartDate && r.Timestamp <= dateRange.EndDate).ToList();

        if (prevRangeReadings.Count > 0 && curRangeReadings.Count > 0)
        {
            var comparison = EnergyAnalyticsEngine.ComparePeriods(curRangeReadings, prevRangeReadings, tariff);
            if (comparison.PercentageChange.HasValue && comparison.PercentageChange.Value > 15.0 && comparison.AbsoluteDifferenceKwh > 5.0)
            {
                decimal? costDiff = comparison.CostDifference;
                string costText = costDiff.HasValue ? $" (estimated +{tariff.Currency}{costDiff.Value:N2})" : "";
                recommendations.Add(new Recommendation
                {
                    Type = RecommendationType.RisingUsage,
                    Title = "Rising Energy Consumption Trend",
                    Description = "Your energy consumption increased notably compared with the preceding comparable timeframe.",
                    Evidence = $"Current Period: {comparison.CurrentTotalKwh:N1} kWh vs Previous Period: {comparison.PreviousTotalKwh:N1} kWh (+{comparison.PercentageChange.Value:N1}% difference){costText}.",
                    Calculation = $"Absolute rise of {comparison.AbsoluteDifferenceKwh:N1} kWh over {periodDays} days.",
                    PracticalSuggestion = "Check seasonal cooling/heating adjustments, verify thermostat setpoints, and check for recurring background appliances.",
                    Priority = 2
                });
            }
        }

        // 3. High Estimated Appliance Usage
        var activeAppliances = appliances.Where(a => a.IncludeInCalculation).ToList();
        if (activeAppliances.Count > 0 && totalKwh > 0)
        {
            foreach (var app in activeAppliances)
            {
                double appKwh = ApplianceCalculator.CalculatePeriodKwh(app, periodDays);
                double share = (appKwh / totalKwh) * 100.0;
                if (share >= 30.0 && !app.IsEssential)
                {
                    decimal appCost = ApplianceCalculator.EstimateAppliancePeriodCost(app, periodDays, tariff.FlatRatePerKwh);
                    recommendations.Add(new Recommendation
                    {
                        Type = RecommendationType.HighApplianceUsage,
                        Title = $"High Share from {app.Name}",
                        Description = $"{app.Name} represents approximately {share:N0}% of your total electricity consumption for this period.",
                        Evidence = $"Estimated usage: {appKwh:N1} kWh ({tariff.Currency}{appCost:N2}) of total {totalKwh:N1} kWh.",
                        Calculation = $"({app.RatedWatts}W × {app.HoursPerDay}h/day × {app.Quantity} qty × duty cycle {app.DutyCycle:N1}) = {appKwh:N1} kWh.",
                        PracticalSuggestion = $"Consider running {app.Name} during off-peak hours, adjusting eco-modes, or reducing operating duration by 1 hour daily.",
                        Priority = 2
                    });
                    break; // Surface one major appliance
                }
            }
        }

        // 4. Standby Load Reduction
        double totalStandbyKwh = activeAppliances.Sum(a =>
            (a.StandbyWatts * a.Quantity * a.StandbyHoursPerDay * periodDays) / 1000.0);
        if (totalStandbyKwh > 5.0 && totalKwh > 0 && (totalStandbyKwh / totalKwh) >= 0.05)
        {
            decimal standbyCost = Math.Round((decimal)totalStandbyKwh * tariff.FlatRatePerKwh, 2);
            recommendations.Add(new Recommendation
            {
                Type = RecommendationType.PotentialStandby,
                Title = "Potential Standby Load Reduction",
                Description = "Identified recurring vampire power draw from devices on standby mode.",
                Evidence = $"Estimated standby consumption: {totalStandbyKwh:N1} kWh costing ~{tariff.Currency}{standbyCost:N2} across {periodDays} days.",
                Calculation = $"Combined standby draw from {activeAppliances.Count(a => a.StandbyWatts > 0)} appliances with non-zero standby wattage.",
                PracticalSuggestion = "Use smart power strips or switch off electronics (TVs, consoles, chargers, desktop monitors) at the wall socket when not in use for extended periods.",
                Priority = 3
            });
        }

        // 5. Peak-Hour Usage under TOU
        if (tariff.Type == TariffType.TimeOfUse && readings.Any(r => r.Timestamp.Hour != 0))
        {
            var peakBand = tariff.TimeBands.OrderByDescending(b => b.RatePerKwh).FirstOrDefault();
            if (peakBand != null)
            {
                var peakReadings = readings.Where(r => peakBand.Matches(r.Timestamp)).ToList();
                double peakKwh = peakReadings.Sum(r => r.ConsumptionKwh);
                double peakShare = totalKwh > 0 ? (peakKwh / totalKwh) * 100.0 : 0.0;

                if (peakShare >= 35.0)
                {
                    decimal peakCost = Math.Round((decimal)peakKwh * peakBand.RatePerKwh, 2);
                    recommendations.Add(new Recommendation
                    {
                        Type = RecommendationType.PeakHourUsage,
                        Title = "High Consumption During Peak Tariff Hours",
                        Description = $"Over {peakShare:N0}% of your electricity consumption occurs during higher-priced peak hours ({peakBand.Name}).",
                        Evidence = $"Peak usage: {peakKwh:N1} kWh at {tariff.Currency}{peakBand.RatePerKwh:N2}/kWh (Total peak cost: {tariff.Currency}{peakCost:N2}).",
                        Calculation = $"Peak hours: {peakBand.StartHour:00}:00 to {peakBand.EndHour:00}:00 on active days.",
                        PracticalSuggestion = "Shift flexible energy tasks such as running the dishwasher, laundry washing, or EV charging to off-peak or normal hours.",
                        Priority = 1
                    });
                }
            }
        }

        // 6. Weekend vs Weekday Usage
        var weekdays = daily.Where(d => !d.IsWeekend).ToList();
        var weekends = daily.Where(d => d.IsWeekend).ToList();
        if (weekdays.Count > 0 && weekends.Count > 0)
        {
            double weekdayAvg = weekdays.Average(d => d.TotalKwh);
            double weekendAvg = weekends.Average(d => d.TotalKwh);
            if (weekdayAvg > 0.001 && ((weekendAvg - weekdayAvg) / weekdayAvg) >= 0.40)
            {
                double pctHigher = ((weekendAvg - weekdayAvg) / weekdayAvg) * 100.0;
                recommendations.Add(new Recommendation
                {
                    Type = RecommendationType.HighWeekendUsage,
                    Title = "Significantly Higher Weekend Consumption",
                    Description = $"Your average daily consumption on weekends is {pctHigher:N0}% higher than weekdays.",
                    Evidence = $"Weekend Daily Avg: {weekendAvg:N1} kWh vs Weekday Daily Avg: {weekdayAvg:N1} kWh.",
                    Calculation = $"Average delta: +{(weekendAvg - weekdayAvg):N1} kWh per weekend day.",
                    PracticalSuggestion = "Review weekend cooking, entertainment, and HVAC habits to optimize energy schedules.",
                    Priority = 3
                });
            }
        }

        // 7. Unallocated Energy Review
        if (activeAppliances.Count > 0 && totalKwh > 10.0)
        {
            var (_, estAppKwh, diff, isOver) = ApplianceCalculator.CompareWithMeter(totalKwh, activeAppliances, periodDays);
            if (!isOver && diff > 0 && (diff / totalKwh) >= 0.30)
            {
                double unallocatedPct = (diff / totalKwh) * 100.0;
                recommendations.Add(new Recommendation
                {
                    Type = RecommendationType.UnallocatedEnergy,
                    Title = "Unallocated Energy Requiring Appliance Review",
                    Description = $"{unallocatedPct:N0}% of measured energy is not accounted for by your tracked appliances.",
                    Evidence = $"Total Meter: {totalKwh:N1} kWh vs Tracked Appliances: {estAppKwh:N1} kWh (Gap: {diff:N1} kWh).",
                    Calculation = "Difference between imported meter readings and calculated appliance inventory.",
                    PracticalSuggestion = "Add additional household devices (refrigerators, secondary freezers, water heaters, HVAC, pumps) to your tracked appliance list to improve analytical accuracy.",
                    Priority = 2
                });
            }
        }

        return recommendations.OrderBy(r => r.Priority).ToList();
    }
}
