using PowerPulse.Core.Enums;

namespace PowerPulse.Core.Calculations;

public static class UnitConverter
{
    public static double ToKWh(double value, EnergyUnit unit)
    {
        return unit switch
        {
            EnergyUnit.Wh => value / 1000.0,
            EnergyUnit.KWh => value,
            EnergyUnit.MWh => value * 1000.0,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), $"Unsupported energy unit: {unit}")
        };
    }

    public static bool TryParseUnit(string? unitStr, out EnergyUnit unit)
    {
        unit = EnergyUnit.KWh;
        if (string.IsNullOrWhiteSpace(unitStr))
            return false;

        var clean = unitStr.Trim().ToLowerInvariant();
        if (clean is "kwh" or "kw.h" or "kw-h" or "kilowatthour" or "kilowatt-hour" or "kilowatt hours")
        {
            unit = EnergyUnit.KWh;
            return true;
        }
        if (clean is "wh" or "w.h" or "w-h" or "watthour" or "watt-hour" or "watt hours")
        {
            unit = EnergyUnit.Wh;
            return true;
        }
        if (clean is "mwh" or "mw.h" or "mw-h" or "megawatthour" or "megawatt-hour")
        {
            unit = EnergyUnit.MWh;
            return true;
        }

        return false;
    }
}
