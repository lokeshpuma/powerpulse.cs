namespace PowerPulse.Core.Models;

public class BillEstimate
{
    public DateTimeOffset BillingPeriodStart { get; set; }
    public DateTimeOffset BillingPeriodEnd { get; set; }
    public double TotalKwh { get; set; }
    public decimal EnergyCharges { get; set; }
    public decimal FixedCharges { get; set; }
    public decimal OtherCharges { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxPercentage { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalEstimatedBill { get; set; }
    public decimal? ActualImportedBill { get; set; }
    public decimal? Difference => ActualImportedBill.HasValue ? TotalEstimatedBill - ActualImportedBill.Value : null;
    public string Currency { get; set; } = "$";

    public List<SlabChargeBreakdown> SlabBreakdowns { get; set; } = new();
    public List<TimeOfUseChargeBreakdown> TimeOfUseBreakdowns { get; set; } = new();
}

public class SlabChargeBreakdown
{
    public int SlabIndex { get; set; }
    public string Label { get; set; } = string.Empty;
    public double KwhInSlab { get; set; }
    public decimal RatePerKwh { get; set; }
    public decimal Amount { get; set; }
}

public class TimeOfUseChargeBreakdown
{
    public string BandName { get; set; } = string.Empty;
    public double KwhInBand { get; set; }
    public decimal RatePerKwh { get; set; }
    public decimal Amount { get; set; }
}
