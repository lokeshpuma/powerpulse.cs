using PowerPulse.Core.Calculations;
using PowerPulse.Core.Enums;
using Xunit;

namespace PowerPulse.Core.Tests;

public class UnitConverterTests
{
    [Fact]
    public void ToKWh_FromWh_DividesBy1000()
    {
        double kwh = UnitConverter.ToKWh(5000, EnergyUnit.Wh);
        Assert.Equal(5.0, kwh);
    }

    [Fact]
    public void ToKWh_FromKWh_RemainsUnchanged()
    {
        double kwh = UnitConverter.ToKWh(12.5, EnergyUnit.KWh);
        Assert.Equal(12.5, kwh);
    }

    [Fact]
    public void ToKWh_FromMWh_MultipliesBy1000()
    {
        double kwh = UnitConverter.ToKWh(1.5, EnergyUnit.MWh);
        Assert.Equal(1500.0, kwh);
    }

    [Theory]
    [InlineData("kwh", EnergyUnit.KWh)]
    [InlineData("KWH", EnergyUnit.KWh)]
    [InlineData("kw.h", EnergyUnit.KWh)]
    [InlineData("wh", EnergyUnit.Wh)]
    [InlineData("WH", EnergyUnit.Wh)]
    [InlineData("mwh", EnergyUnit.MWh)]
    [InlineData("MWH", EnergyUnit.MWh)]
    public void TryParseUnit_ValidStrings_ReturnsTrue(string input, EnergyUnit expected)
    {
        bool success = UnitConverter.TryParseUnit(input, out var unit);
        Assert.True(success);
        Assert.Equal(expected, unit);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("volts")]
    [InlineData("unknown")]
    public void TryParseUnit_InvalidStrings_ReturnsFalse(string? input)
    {
        bool success = UnitConverter.TryParseUnit(input, out _);
        Assert.False(success);
    }
}
