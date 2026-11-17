namespace PowerPulse.Core.Enums;

public enum TariffType
{
    Flat = 1,
    Slab = 2,
    TimeOfUse = 3
}

public enum EnergyUnit
{
    Wh = 1,
    KWh = 2,
    MWh = 3
}

public enum ApplianceCategory
{
    Refrigerator = 1,
    AirConditioner = 2,
    Television = 3,
    Fan = 4,
    Lighting = 5,
    WashingMachine = 6,
    Dishwasher = 7,
    Laptop = 8,
    DesktopComputer = 9,
    Microwave = 10,
    WaterHeater = 11,
    Other = 12
}

public enum RecommendationType
{
    AnomalySpike = 1,
    RisingUsage = 2,
    HighApplianceUsage = 3,
    PotentialStandby = 4,
    PeakHourUsage = 5,
    HighWeekendUsage = 6,
    UnallocatedEnergy = 7
}

public enum ForecastMethod
{
    AverageDaily = 1,
    RecentSevenDays = 2
}

public enum DateRangeOption
{
    Last7Days = 1,
    Last30Days = 2,
    CurrentMonth = 3,
    PreviousMonth = 4,
    CurrentBillingCycle = 5,
    Custom = 6
}

public enum ImportStatus
{
    Success = 1,
    Warnings = 2,
    Failed = 3
}

public enum ReadingType
{
    Interval = 1,
    Daily = 2,
    Cumulative = 3
}
