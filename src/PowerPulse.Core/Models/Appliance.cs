using PowerPulse.Core.Enums;

namespace PowerPulse.Core.Models;

public class Appliance
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public ApplianceCategory Category { get; set; } = ApplianceCategory.Other;
    public string? CustomCategoryName { get; set; }
    public double RatedWatts { get; set; }
    public int Quantity { get; set; } = 1;
    public double HoursPerDay { get; set; }
    public double UsageDaysPerWeek { get; set; } = 7.0;
    public double StandbyWatts { get; set; } = 0.0;
    public double StandbyHoursPerDay { get; set; } = 0.0;
    public double DutyCycle { get; set; } = 1.0; // 0.0 to 1.0
    public bool IsEssential { get; set; } = false;
    public bool IncludeInCalculation { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string DisplayCategory => Category == ApplianceCategory.Other && !string.IsNullOrWhiteSpace(CustomCategoryName)
        ? CustomCategoryName
        : Category.ToString();
}
