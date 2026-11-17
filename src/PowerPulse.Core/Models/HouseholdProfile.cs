namespace PowerPulse.Core.Models;

public class HouseholdProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Home";
    public string Country { get; set; } = "United States";
    public string Currency { get; set; } = "$";
    public string TimeZoneId { get; set; } = "UTC";
    public int BillingCycleStartDay { get; set; } = 1;
    public bool IsOnboarded { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
