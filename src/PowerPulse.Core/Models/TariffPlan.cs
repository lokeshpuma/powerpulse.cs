using PowerPulse.Core.Enums;

namespace PowerPulse.Core.Models;

public class TariffPlan
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Default Residential Tariff";
    public TariffType Type { get; set; } = TariffType.Flat;
    public decimal FlatRatePerKwh { get; set; } = 0.15m;
    public List<TariffSlab> Slabs { get; set; } = new();
    public List<TimeOfUseBand> TimeBands { get; set; } = new();
    public decimal FixedCharge { get; set; } = 10.00m;
    public decimal TaxPercentage { get; set; } = 5.0m;
    public decimal OtherCharges { get; set; } = 0.00m;
    public string Currency { get; set; } = "$";
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;

    public static TariffPlan CreateDefault()
    {
        return new TariffPlan
        {
            Type = TariffType.Flat,
            FlatRatePerKwh = 0.15m,
            FixedCharge = 12.50m,
            TaxPercentage = 5.0m,
            OtherCharges = 0m,
            Currency = "$",
            Slabs = new List<TariffSlab>
            {
                new() { SlabIndex = 1, FromKwh = 0, ToKwh = 100, RatePerKwh = 0.10m },
                new() { SlabIndex = 2, FromKwh = 100, ToKwh = 300, RatePerKwh = 0.14m },
                new() { SlabIndex = 3, FromKwh = 300, ToKwh = 500, RatePerKwh = 0.18m },
                new() { SlabIndex = 4, FromKwh = 500, ToKwh = null, RatePerKwh = 0.22m }
            },
            TimeBands = new List<TimeOfUseBand>
            {
                new()
                {
                    Name = "Off-Peak",
                    StartHour = 22,
                    EndHour = 6,
                    RatePerKwh = 0.08m,
                    DaysOfWeek = new() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday }
                },
                new()
                {
                    Name = "Normal",
                    StartHour = 6,
                    EndHour = 14,
                    RatePerKwh = 0.14m,
                    DaysOfWeek = new() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }
                },
                new()
                {
                    Name = "Peak",
                    StartHour = 14,
                    EndHour = 20,
                    RatePerKwh = 0.24m,
                    DaysOfWeek = new() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }
                },
                new()
                {
                    Name = "Normal",
                    StartHour = 20,
                    EndHour = 22,
                    RatePerKwh = 0.14m,
                    DaysOfWeek = new() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }
                },
                new()
                {
                    Name = "Weekend Normal",
                    StartHour = 6,
                    EndHour = 22,
                    RatePerKwh = 0.11m,
                    DaysOfWeek = new() { DayOfWeek.Saturday, DayOfWeek.Sunday }
                }
            }
        };
    }
}

public class TariffSlab
{
    public int SlabIndex { get; set; }
    public double FromKwh { get; set; }
    public double? ToKwh { get; set; } // null means unlimited
    public decimal RatePerKwh { get; set; }
}

public class TimeOfUseBand
{
    public string Name { get; set; } = "Normal";
    public int StartHour { get; set; } // 0-23
    public int EndHour { get; set; }   // 0-23 (if EndHour < StartHour, wraps midnight)
    public decimal RatePerKwh { get; set; }
    public List<DayOfWeek> DaysOfWeek { get; set; } = new();

    public bool Matches(DateTimeOffset timestamp)
    {
        if (DaysOfWeek.Count > 0 && !DaysOfWeek.Contains(timestamp.DayOfWeek))
            return false;

        int hour = timestamp.Hour;
        if (StartHour <= EndHour)
        {
            return hour >= StartHour && hour < EndHour;
        }
        else
        {
            // Wraps midnight (e.g. 22 to 6 means hour >= 22 OR hour < 6)
            return hour >= StartHour || hour < EndHour;
        }
    }
}
