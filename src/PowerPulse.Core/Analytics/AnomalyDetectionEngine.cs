using PowerPulse.Core.Models;

namespace PowerPulse.Core.Analytics;

public static class AnomalyDetectionEngine
{
    public static List<AnomalyResult> DetectAnomalies(
        IEnumerable<DailyEnergyAggregation> dailyData,
        int windowSize = 14,
        double threshold = 3.5)
    {
        var days = dailyData.OrderBy(d => d.Date).ToList();
        var results = new List<AnomalyResult>();

        if (days.Count < windowSize)
        {
            // Insufficient baseline
            foreach (var d in days)
            {
                results.Add(new AnomalyResult
                {
                    Date = d.Date,
                    ConsumptionKwh = d.TotalKwh,
                    RollingMedianKwh = d.TotalKwh,
                    DifferenceKwh = 0,
                    PercentageDifference = 0,
                    RobustZScore = 0,
                    Reason = $"Baseline insufficient (< {windowSize} days observed)",
                    IsAnomaly = false
                });
            }
            return results;
        }

        for (int i = 0; i < days.Count; i++)
        {
            var currentDay = days[i];

            // Establish rolling window preceding current day (up to windowSize days)
            int windowStart = Math.Max(0, i - windowSize);
            var window = days.Skip(windowStart).Take(i - windowStart).Select(d => d.TotalKwh).ToList();

            if (window.Count < Math.Min(7, windowSize / 2))
            {
                // Not enough prior days yet
                results.Add(new AnomalyResult
                {
                    Date = currentDay.Date,
                    ConsumptionKwh = currentDay.TotalKwh,
                    RollingMedianKwh = currentDay.TotalKwh,
                    DifferenceKwh = 0,
                    PercentageDifference = 0,
                    RobustZScore = 0,
                    Reason = "Establishing baseline",
                    IsAnomaly = false
                });
                continue;
            }

            double median = CalculateMedian(window);
            double mad = CalculateMAD(window, median);

            double diff = currentDay.TotalKwh - median;
            double pctDiff = median > 0.0001 ? (diff / median) * 100.0 : 0.0;
            double robustZScore = 0.0;
            bool isAnomaly = false;
            string reason = "Normal usage within statistical expectations";

            if (mad > 0.0001)
            {
                robustZScore = 0.6745 * diff / mad;
                if (Math.Abs(robustZScore) >= threshold)
                {
                    isAnomaly = true;
                    if (robustZScore > 0)
                    {
                        reason = $"Consumption spike detected ({currentDay.TotalKwh:N1} kWh vs median {median:N1} kWh, +{pctDiff:N0}%, Z-Score: {robustZScore:N1})";
                    }
                    else
                    {
                        reason = $"Unusually low consumption detected ({currentDay.TotalKwh:N1} kWh vs median {median:N1} kWh, {pctDiff:N0}%, Z-Score: {robustZScore:N1})";
                    }
                }
            }
            else
            {
                // MAD is zero (all prior window values are identical)
                if (Math.Abs(diff) > 2.0)
                {
                    isAnomaly = true;
                    reason = $"Usage diverged from zero-variance baseline ({currentDay.TotalKwh:N1} kWh vs constant {median:N1} kWh)";
                    robustZScore = diff > 0 ? threshold + 1 : -threshold - 1;
                }
            }

            results.Add(new AnomalyResult
            {
                Date = currentDay.Date,
                ConsumptionKwh = currentDay.TotalKwh,
                RollingMedianKwh = Math.Round(median, 2),
                DifferenceKwh = Math.Round(diff, 2),
                PercentageDifference = Math.Round(pctDiff, 1),
                RobustZScore = Math.Round(robustZScore, 2),
                Reason = reason,
                DetectionMethod = "Rolling MAD (Median Absolute Deviation)",
                IsAnomaly = isAnomaly
            });
        }

        return results;
    }

    public static double CalculateMedian(List<double> numbers)
    {
        if (numbers == null || numbers.Count == 0)
            return 0.0;

        var sorted = numbers.OrderBy(n => n).ToList();
        int count = sorted.Count;
        if (count % 2 == 1)
        {
            return sorted[count / 2];
        }
        else
        {
            return (sorted[(count / 2) - 1] + sorted[count / 2]) / 2.0;
        }
    }

    public static double CalculateMAD(List<double> numbers, double median)
    {
        if (numbers == null || numbers.Count == 0)
            return 0.0;

        var absoluteDeviations = numbers.Select(n => Math.Abs(n - median)).ToList();
        return CalculateMedian(absoluteDeviations);
    }
}
