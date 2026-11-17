using PowerPulse.Core.Enums;

namespace PowerPulse.Core.Models;

public class Recommendation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public RecommendationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
    public string Calculation { get; set; } = string.Empty;
    public string PracticalSuggestion { get; set; } = string.Empty;
    public int Priority { get; set; } = 2; // 1 = High, 2 = Medium, 3 = Low
    public bool IsReviewed { get; set; } = false;
    public bool IsDismissed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
