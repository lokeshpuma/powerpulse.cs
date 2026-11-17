using PowerPulse.Core.Analytics;
using PowerPulse.Core.Models;
using Xunit;

namespace PowerPulse.Core.Tests;

public class AnomalyDetectionTests
{
    [Fact]
    public void CalculateMedian_OddAndEvenCounts_ReturnsExpected()
    {
        var oddList = new List<double> { 5.0, 1.0, 9.0 };
        Assert.Equal(5.0, AnomalyDetectionEngine.CalculateMedian(oddList));

        var evenList = new List<double> { 1.0, 3.0, 5.0, 7.0 };
        Assert.Equal(4.0, AnomalyDetectionEngine.CalculateMedian(evenList));
    }

    [Fact]
    public void CalculateMAD_StandardNumbers_CalculatesAbsoluteDeviations()
    {
        // Numbers: 2, 4, 4, 4, 5, 5, 7, 9
        // Median = 4.5
        // Absolute deviations: 2.5, 0.5, 0.5, 0.5, 0.5, 0.5, 2.5, 4.5
        // Sorted: 0.5, 0.5, 0.5, 0.5, 0.5, 2.5, 2.5, 4.5
        // Median of deviations = (0.5 + 0.5) / 2 = 0.5
        var numbers = new List<double> { 2, 4, 4, 4, 5, 5, 7, 9 };
        double median = AnomalyDetectionEngine.CalculateMedian(numbers);
        double mad = AnomalyDetectionEngine.CalculateMAD(numbers, median);

        Assert.Equal(4.5, median);
        Assert.Equal(0.5, mad);
    }

    [Fact]
    public void DetectAnomalies_WithSpikeExceedingThreshold_FlagsAnomaly()
    {
        var dailyData = new List<DailyEnergyAggregation>();
        var baseDate = new DateOnly(2026, 1, 1);

        // 20 days of steady 10 kWh consumption with minor variance
        for (int i = 0; i < 20; i++)
        {
            dailyData.Add(new DailyEnergyAggregation
            {
                Date = baseDate.AddDays(i),
                TotalKwh = 10.0 + (i % 2 == 0 ? 0.2 : -0.2)
            });
        }

        // Day 21 has an extreme spike of 35 kWh
        dailyData.Add(new DailyEnergyAggregation
        {
            Date = baseDate.AddDays(20),
            TotalKwh = 35.0
        });

        var results = AnomalyDetectionEngine.DetectAnomalies(dailyData, windowSize: 14, threshold: 3.5);

        var spikeResult = results.FirstOrDefault(r => r.Date == baseDate.AddDays(20));
        Assert.NotNull(spikeResult);
        Assert.True(spikeResult.IsAnomaly);
        Assert.True(spikeResult.RobustZScore > 3.5);
    }

    [Fact]
    public void DetectAnomalies_InsufficientBaseline_DoesNotFlagFalsePositive()
    {
        var dailyData = new List<DailyEnergyAggregation>();
        var baseDate = new DateOnly(2026, 1, 1);

        // Only 5 days (insufficient baseline < 14)
        for (int i = 0; i < 5; i++)
        {
            dailyData.Add(new DailyEnergyAggregation
            {
                Date = baseDate.AddDays(i),
                TotalKwh = 10.0
            });
        }

        var results = AnomalyDetectionEngine.DetectAnomalies(dailyData, windowSize: 14, threshold: 3.5);
        Assert.All(results, r => Assert.False(r.IsAnomaly));
    }
}
