using System.Text;
using PowerPulse.Core.Models;
using PowerPulse.Core.Calculations;

namespace PowerPulse.Infrastructure.Export;

public static class ReportExportService
{
    public static string GenerateCsvReport(
        DateRangeFilter dateRange,
        PeriodComparisonResult comparison,
        List<DailyEnergyAggregation> dailyData,
        List<Appliance> appliances,
        List<AnomalyResult> anomalies,
        ForecastResult? forecast,
        List<Recommendation> recommendations,
        TariffPlan tariff,
        CarbonFactor? carbonFactor)
    {
        var sb = new StringBuilder();

        sb.AppendLine("PowerPulse Household Energy Intelligence Report");
        sb.AppendLine($"Report Date Range,{dateRange.StartDate:yyyy-MM-dd} to {dateRange.EndDate:yyyy-MM-dd}");
        sb.AppendLine($"Generated At (UTC),{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        // Key Metrics
        sb.AppendLine("=== KEY CONSUMPTION METRICS ===");
        sb.AppendLine("Metric,Value,Unit,Label");
        sb.AppendLine($"Total Energy Consumed,{comparison.CurrentTotalKwh:N2},kWh,Actual Measured");
        sb.AppendLine($"Previous Period Consumption,{comparison.PreviousTotalKwh:N2},kWh,Actual Measured");
        sb.AppendLine($"Period Change (Absolute),{comparison.AbsoluteDifferenceKwh:N2},kWh,Calculated");
        sb.AppendLine($"Period Change (Percentage),{(comparison.PercentageChange.HasValue ? comparison.PercentageChange.Value.ToString("N1") + "%" : "N/A")},%,Calculated");
        sb.AppendLine($"Daily Average Consumption,{comparison.CurrentDailyAverageKwh:N2},kWh/day,Calculated");
        sb.AppendLine($"Weekday Daily Average,{comparison.WeekdayAverageDailyKwh:N2},kWh/day,Calculated");
        sb.AppendLine($"Weekend Daily Average,{comparison.WeekendAverageDailyKwh:N2},kWh/day,Calculated");
        sb.AppendLine($"Estimated Electricity Cost,{(comparison.CurrentEstimatedCost.HasValue ? tariff.Currency + comparison.CurrentEstimatedCost.Value.ToString("N2") : "Not Configured")},{tariff.Currency},Estimated");
        sb.AppendLine($"Carbon Emissions Impact,{(comparison.CurrentCarbonKg.HasValue ? comparison.CurrentCarbonKg.Value.ToString("N2") : "Not Configured")},kg CO2e,Estimated");
        sb.AppendLine();

        // Daily Aggregations
        sb.AppendLine("=== DAILY CONSUMPTION BREAKDOWN ===");
        sb.AppendLine("Date,Total kWh,Day of Week,Min Hourly kWh,Max Hourly kWh,Average Hourly kWh");
        foreach (var d in dailyData)
        {
            sb.AppendLine($"{d.Date:yyyy-MM-dd},{d.TotalKwh:N3},{d.Date.DayOfWeek},{d.MinHourlyKwh:N3},{d.MaxHourlyKwh:N3},{d.AvgHourlyKwh:N3}");
        }
        sb.AppendLine();

        // Tracked Appliances
        sb.AppendLine("=== TRACKED APPLIANCE INVENTORY ===");
        sb.AppendLine("Appliance Name,Category,Rated Power (Watts),Quantity,Daily Hours,Days/Week,Standby (Watts),Estimated Period kWh,Estimated Cost,Essential");
        int periodDays = Math.Max(1, dailyData.Count);
        foreach (var app in appliances.Where(a => a.IncludeInCalculation))
        {
            double appKwh = ApplianceCalculator.CalculatePeriodKwh(app, periodDays);
            decimal appCost = ApplianceCalculator.EstimateAppliancePeriodCost(app, periodDays, tariff.FlatRatePerKwh);
            sb.AppendLine($"{SanitizeCsv(app.Name)},{SanitizeCsv(app.DisplayCategory)},{app.RatedWatts},{app.Quantity},{app.HoursPerDay},{app.UsageDaysPerWeek},{app.StandbyWatts},{appKwh:N2},{tariff.Currency}{appCost:N2},{(app.IsEssential ? "Yes" : "No")}");
        }
        sb.AppendLine();

        // Anomalies
        sb.AppendLine("=== DETECTED ANOMALIES ===");
        sb.AppendLine("Date,Measured kWh,Historical Median kWh,Difference kWh,Robust Z-Score,Alert Reason");
        foreach (var a in anomalies.Where(a => a.IsAnomaly))
        {
            sb.AppendLine($"{a.Date:yyyy-MM-dd},{a.ConsumptionKwh:N2},{a.RollingMedianKwh:N2},{a.DifferenceKwh:N2},{a.RobustZScore:N2},{SanitizeCsv(a.Reason)}");
        }
        sb.AppendLine();

        // Forecast
        if (forecast != null)
        {
            sb.AppendLine("=== BILLING CYCLE FORECAST ===");
            sb.AppendLine("Metric,Value,Unit");
            sb.AppendLine($"Observed Cycle Usage,{forecast.ObservedKwh:N2},kWh");
            sb.AppendLine($"Remaining Days,{forecast.RemainingDays},days");
            sb.AppendLine($"Forecast Method,{forecast.MethodUsed},Method");
            sb.AppendLine($"Projected Total Usage,{forecast.ProjectedTotalKwh:N2},kWh (Projected)");
            sb.AppendLine($"Projected Bill,{tariff.Currency}{forecast.ProjectedBill:N2},{tariff.Currency} (Estimated)");
            sb.AppendLine();
        }

        // Recommendations
        sb.AppendLine("=== SMART RECOMMENDATIONS ===");
        sb.AppendLine("Title,Priority,Evidence,Practical Suggestion");
        foreach (var r in recommendations.Where(r => !r.IsDismissed))
        {
            sb.AppendLine($"{SanitizeCsv(r.Title)},Priority {r.Priority},{SanitizeCsv(r.Evidence)},{SanitizeCsv(r.PracticalSuggestion)}");
        }
        sb.AppendLine();

        sb.AppendLine("=== METHODOLOGY & LIMITATIONS ===");
        sb.AppendLine("Notice,All electricity bill figures and appliance energy amounts are estimates based on user configuration.");
        sb.AppendLine("Notice,Do not treat estimated bills as official utility invoices. Environmental emissions are estimated based on regional grid emission averages.");

        return sb.ToString();
    }

    public static string GenerateHtmlReport(
        HouseholdProfile profile,
        DateRangeFilter dateRange,
        PeriodComparisonResult comparison,
        List<DailyEnergyAggregation> dailyData,
        List<Appliance> appliances,
        List<AnomalyResult> anomalies,
        ForecastResult? forecast,
        List<Recommendation> recommendations,
        TariffPlan tariff,
        CarbonFactor? carbonFactor)
    {
        var sb = new StringBuilder();
        int periodDays = Math.Max(1, dailyData.Count);

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\" />");
        sb.AppendLine("<title>PowerPulse Energy Report - " + System.Net.WebUtility.HtmlEncode(profile.Name) + "</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; margin: 40px; color: #03050B; background: #FFF; line-height: 1.5; }");
        sb.AppendLine(".header { border-bottom: 2px solid #1DA058; padding-bottom: 16px; margin-bottom: 24px; display: flex; justify-content: space-between; align-items: flex-end; }");
        sb.AppendLine(".title { font-size: 26px; font-weight: 700; color: #03050B; margin: 0; }");
        sb.AppendLine(".subtitle { font-size: 14px; color: #5A7E9B; margin-top: 4px; }");
        sb.AppendLine(".badge { display: inline-block; padding: 4px 10px; border-radius: 6px; font-size: 12px; font-weight: 600; background: #E8F5E9; color: #1E6A3F; }");
        sb.AppendLine(".grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 30px; }");
        sb.AppendLine(".card { background: #F8F7FA; border: 1px solid #E2E8F0; border-radius: 8px; padding: 16px; }");
        sb.AppendLine(".card-title { font-size: 12px; text-transform: uppercase; color: #5A7E9B; font-weight: 600; margin-bottom: 6px; }");
        sb.AppendLine(".card-value { font-size: 24px; font-weight: 700; color: #03050B; }");
        sb.AppendLine(".card-meta { font-size: 12px; color: #718096; margin-top: 4px; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 12px; margin-bottom: 28px; font-size: 13px; }");
        sb.AppendLine("th { background: #F1F5F9; text-align: left; padding: 10px; border: 1px solid #CBD5E1; font-weight: 600; }");
        sb.AppendLine("td { padding: 8px 10px; border: 1px solid #E2E8F0; }");
        sb.AppendLine("tr:nth-child(even) { background: #F8FAFC; }");
        sb.AppendLine(".section-title { font-size: 18px; font-weight: 700; color: #03050B; margin-top: 24px; margin-bottom: 8px; border-bottom: 1px solid #E2E8F0; padding-bottom: 6px; }");
        sb.AppendLine(".notice-box { background: #FEF3C7; border: 1px solid #F59E0B; border-radius: 6px; padding: 12px; font-size: 12px; color: #92400E; margin-top: 30px; }");
        sb.AppendLine("@media print { body { margin: 15mm; } .no-print { display: none; } }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine("<div class=\"header\">");
        sb.AppendLine("<div>");
        sb.AppendLine("<h1 class=\"title\">PowerPulse Energy Report</h1>");
        sb.AppendLine($"<div class=\"subtitle\">Household: <strong>{System.Net.WebUtility.HtmlEncode(profile.Name)}</strong> | Date Range: {dateRange.StartDate:MMM dd, yyyy} – {dateRange.EndDate:MMM dd, yyyy}</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div>");
        sb.AppendLine($"<span class=\"badge\">Generated {DateTime.UtcNow:MMM dd, yyyy}</span>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        // KPI Summary Cards
        sb.AppendLine("<div class=\"grid\">");
        sb.AppendLine("<div class=\"card\">");
        sb.AppendLine("<div class=\"card-title\">Total Consumption</div>");
        sb.AppendLine($"<div class=\"card-value\">{comparison.CurrentTotalKwh:N1} <span style=\"font-size:14px;font-weight:normal;\">kWh</span></div>");
        sb.AppendLine($"<div class=\"card-meta\">{(comparison.PercentageChange.HasValue ? (comparison.PercentageChange.Value >= 0 ? "+" : "") + comparison.PercentageChange.Value.ToString("N1") + "% vs prev period" : "Baseline n/a")}</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"card\">");
        sb.AppendLine("<div class=\"card-title\">Estimated Electricity Cost</div>");
        string costVal = comparison.CurrentEstimatedCost.HasValue ? $"{tariff.Currency}{comparison.CurrentEstimatedCost.Value:N2}" : "—";
        sb.AppendLine($"<div class=\"card-value\">{costVal}</div>");
        sb.AppendLine($"<div class=\"card-meta\">Tariff: {tariff.Type} ({tariff.Currency}{tariff.FlatRatePerKwh:N2}/kWh base)</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"card\">");
        sb.AppendLine("<div class=\"card-title\">Daily Average</div>");
        sb.AppendLine($"<div class=\"card-value\">{comparison.CurrentDailyAverageKwh:N1} <span style=\"font-size:14px;font-weight:normal;\">kWh/d</span></div>");
        sb.AppendLine($"<div class=\"card-meta\">Wkday: {comparison.WeekdayAverageDailyKwh:N1} | Wkend: {comparison.WeekendAverageDailyKwh:N1} kWh</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"card\">");
        sb.AppendLine("<div class=\"card-title\">Carbon Footprint</div>");
        string carbonVal = comparison.CurrentCarbonKg.HasValue ? $"{comparison.CurrentCarbonKg.Value:N1} kg" : "—";
        sb.AppendLine($"<div class=\"card-value\">{carbonVal}</div>");
        sb.AppendLine($"<div class=\"card-meta\">Factor: {(carbonFactor?.KgCo2ePerKwh ?? 0):N3} kg/kWh ({carbonFactor?.SourceName ?? "None"})</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        // Appliances Table
        sb.AppendLine("<div class=\"section-title\">Tracked Appliance Inventory</div>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Appliance</th><th>Category</th><th>Power</th><th>Operating Time</th><th>Period kWh</th><th>Period Cost</th><th>Essential</th></tr></thead>");
        sb.AppendLine("<tbody>");
        foreach (var a in appliances.Where(a => a.IncludeInCalculation))
        {
            double appKwh = ApplianceCalculator.CalculatePeriodKwh(a, periodDays);
            decimal appCost = ApplianceCalculator.EstimateAppliancePeriodCost(a, periodDays, tariff.FlatRatePerKwh);
            sb.AppendLine($"<tr><td><strong>{System.Net.WebUtility.HtmlEncode(a.Name)}</strong></td><td>{System.Net.WebUtility.HtmlEncode(a.DisplayCategory)}</td><td>{a.RatedWatts:N0} W</td><td>{a.HoursPerDay:N1} h/day ({a.UsageDaysPerWeek:N0} d/wk)</td><td>{appKwh:N1} kWh</td><td>{tariff.Currency}{appCost:N2}</td><td>{(a.IsEssential ? "<span style=\"color:#B92F2D;font-weight:600;\">Yes</span>" : "No")}</td></tr>");
        }
        sb.AppendLine("</tbody></table>");

        // Anomalies Table
        var detectedAnomalies = anomalies.Where(a => a.IsAnomaly).ToList();
        if (detectedAnomalies.Count > 0)
        {
            sb.AppendLine("<div class=\"section-title\">Detected Anomalies</div>");
            sb.AppendLine("<table>");
            sb.AppendLine("<thead><tr><th>Date</th><th>Measured kWh</th><th>Historical Median</th><th>Variance</th><th>Z-Score</th><th>Alert Details</th></tr></thead>");
            sb.AppendLine("<tbody>");
            foreach (var an in detectedAnomalies)
            {
                sb.AppendLine($"<tr><td>{an.Date:yyyy-MM-dd}</td><td>{an.ConsumptionKwh:N1} kWh</td><td>{an.RollingMedianKwh:N1} kWh</td><td>{(an.DifferenceKwh >= 0 ? "+" : "")}{an.DifferenceKwh:N1} kWh ({an.PercentageDifference:N0}%)</td><td>{an.RobustZScore:N1}</td><td>{System.Net.WebUtility.HtmlEncode(an.Reason)}</td></tr>");
            }
            sb.AppendLine("</tbody></table>");
        }

        // Recommendations
        var activeRecs = recommendations.Where(r => !r.IsDismissed).ToList();
        if (activeRecs.Count > 0)
        {
            sb.AppendLine("<div class=\"section-title\">Smart Recommendations</div>");
            sb.AppendLine("<table>");
            sb.AppendLine("<thead><tr><th>Title</th><th>Evidence & Calculation</th><th>Practical Suggestion</th></tr></thead>");
            sb.AppendLine("<tbody>");
            foreach (var rec in activeRecs)
            {
                sb.AppendLine($"<tr><td><strong>{System.Net.WebUtility.HtmlEncode(rec.Title)}</strong></td><td>{System.Net.WebUtility.HtmlEncode(rec.Evidence)}<br/><small style=\"color:#64748B;\">{System.Net.WebUtility.HtmlEncode(rec.Calculation)}</small></td><td>{System.Net.WebUtility.HtmlEncode(rec.PracticalSuggestion)}</td></tr>");
            }
            sb.AppendLine("</tbody></table>");
        }

        // Disclaimer
        sb.AppendLine("<div class=\"notice-box\">");
        sb.AppendLine("<strong>Methodology and Limitations Notice:</strong> PowerPulse is an analytical tool. Bill calculations are estimates based on your configured tariffs and do not constitute legal or utility invoices. Appliance energy consumption figures are calculated estimates based on user-supplied power ratings and usage hours. Carbon emissions are calculated using published regional grid intensity factors.");
        sb.AppendLine("</div>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string SanitizeCsv(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return "\"\"";

        string val = input.Replace("\"", "\"\"");

        // Formula injection protection: if first character is =, +, -, @, prepend a single quote
        if (val.StartsWith('=') || val.StartsWith('+') || val.StartsWith('-') || val.StartsWith('@'))
        {
            val = "'" + val;
        }

        return $"\"{val}\"";
    }
}
