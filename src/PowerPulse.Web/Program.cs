using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PowerPulse.Core.Interfaces;
using PowerPulse.Infrastructure.Csv;
using PowerPulse.Infrastructure.Storage;
using PowerPulse.ML.Forecasting;
using PowerPulse.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IStorageService, IndexedDbStorageService>();
builder.Services.AddScoped<ICsvImportService, CsvImportService>();
builder.Services.AddScoped<ITimeSeriesForecaster, StatisticalForecaster>();

await builder.Build().RunAsync();
