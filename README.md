# PowerPulse ⚡
### Personal Household Energy Consumption Intelligence Platform

[![Build and Deploy](https://github.com/lokeshpuma/powerpulse-cs/actions/workflows/deploy.yml/badge.svg)](https://github.com/lokeshpuma/powerpulse-cs/actions/workflows/deploy.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**PowerPulse** is an intelligent, privacy-focused household energy analytics web platform. It transforms raw electricity meter readings and appliance schedules into clear, actionable efficiency insights, multi-tier electricity bill projections, carbon impact estimations, and statistical anomaly detection.

The application runs entirely client-side as a standalone **Blazor WebAssembly** platform hosted on **GitHub Pages**, persisting data locally in the browser via **IndexedDB** with zero external cloud transmission.

🌐 **Live Application:** [https://lokeshpuma.github.io/powerpulse-cs/](https://lokeshpuma.github.io/powerpulse-cs/)

---

## 🌟 Key Features & Capabilities

### 1. 📊 Executive Dashboard Overview
- **Dynamic Time-Aware Greeting:** Personalized greeting based on local time and profile name.
- **KPI Indicator Cards:** Distinguishes **Actual Measured** consumption, **Estimated** bills, **Calculated** trends, and **Estimated CO2e** carbon footprint.
- **Interactive Energy Curve:** Chart.js powered line chart with granularity controls (Hourly / Daily) and previous period baseline comparisons.
- **Circular Spend Analyzer:** Doughnut breakdown of energy charges, customer fixed charges, and regulatory taxes.
- **Tracked Appliances Summary:** Quick-view of major household loads with calculation toggles.
- **Smart Choices Panel:** Top 3 prioritized, data-driven recommendations.

### 2. ⚡ Energy Usage Analytics
- **24-Hour Diurnal Load Profile:** Visualizes average hourly consumption patterns ($00:00 - 23:00$) to pinpoint morning routines and evening peaks.
- **Daily Usage Breakdown:** Color-coded daily bars distinguishing weekdays from weekends.
- **Monthly Consumption Trends:** Historical month-over-month totals, day counts, daily averages, and estimated electricity costs.
- **Hourly Heatmap:** Day-of-week by hour-of-day intensity matrix reflecting load distribution.

### 3. 🔌 Appliance Management & Meter Reconciliation
- **Detailed Appliance Inventory:** Track power ratings (watts), quantity, daily hours, operating days/week, standby draw, and duty cycles across 12 standard categories.
- **Essential Appliance Safety Protection:** Flag critical equipment (medical life-support, refrigerators) to ensure efficiency tips never suggest switching them off.
- **Meter vs. Appliance Reconciliation:** Automatically compares physical meter readings against calculated appliance inventories, quantifying unallocated or overestimated energy.

### 4. 📁 Robust CSV Data Import Engine
- **Versatile Format Support:** Ingests interval readings, daily totals, and cumulative meter registers.
- **Automatic Delimiter Detection:** Automatically detects comma (`,`), semicolon (`;`), or tab (`\t`) delimiters and handles UTF-8 / UTF-8 BOM encoding.
- **Flexible Column Mapping & Unit Normalization:** Seamlessly maps columns and normalizes Watt-hours (Wh), Kilowatt-hours (kWh), and Megawatt-hours (MWh).
- **Comprehensive Validation:** Detects duplicate timestamps, filters negative consumption, flags cumulative meter rollover resets, and provides a 20-row live preview.
- **Sample Demonstrations:** 1-click loading for 30-day hourly, 90-day daily, and 60-day cumulative meter demo datasets.

### 5. 💰 Tariff & Electricity Bill Estimator
- **Multi-Structure Tariff Engine:**
  - **Flat Rate:** Uniform price per kWh.
  - **Tiered Slabs:** Progressive consumption tiers (e.g., $0-100\text{ kWh}$, $101-300\text{ kWh}$, $>300\text{ kWh}$).
  - **Time-of-Use (TOU):** Peak, normal, and off-peak rate bands by day of week and time window.
- **Accurate Arithmetic:** Decimal precision for energy charges, customer fixed access charges, and percentage taxes.
- **Invoice Reconciliation:** Compares estimated charges against actual utility bill invoices when cost data is imported.

### 6. 🔍 Statistical Anomaly Detection & Insights
- **Rolling Median & Median Absolute Deviation (MAD):**
  $$\text{MAD} = \text{Median}(|\text{Daily Usage} - \text{Rolling Median}|)$$
  $$\text{Robust Z-Score} = 0.6745 \times \frac{\text{Observed Usage} - \text{Rolling Median}}{\text{MAD}}$$
- **Configurable Sensitivity:** Threshold adjustment ($Z \ge 3.5$) with baseline requirements ($\ge 14\text{ days}$) and zero-variance protection.
- **Evidence-Based Recommendations:** Identifies usage spikes, rising consumption, excessive appliance share, vampire standby power, TOU peak concentration, and weekend surges.

### 7. 📑 Analytical Reports & Data Export
- **Formula-Injection-Safe CSV:** Sanitizes spreadsheet formula triggers (`=`, `+`, `-`, `@`).
- **Print-Friendly HTML / PDF:** Dedicated print stylesheet supporting browser **Save as PDF**.
- **JSON System Backups:** Full-state export and restore for household profiles, tariffs, appliances, datasets, and readings.

### 8. ⚙️ Settings, Privacy & Local Storage
- **100% Client-Side Privacy:** All data remains strictly within the browser's IndexedDB origin.
- **Full Data Control:** Clear specific datasets, reset appliance inventories, or wipe the local database with confirmation dialogues.

---

## 🛠️ Technology Stack

| Layer | Technology | Purpose |
|---|---|---|
| **Language** | C# 13 / .NET 10 | Primary language & high-performance calculation engine |
| **Frontend** | Blazor WebAssembly (Standalone) | Modern component-driven single-page web app |
| **Data Parsing** | CsvHelper 33.0.1 | RFC 4180 compliant CSV parsing |
| **Charts** | Chart.js 4.4.7 (Locally Bundled) | Responsive charts with zero external runtime CDN dependencies |
| **Local Storage** | IndexedDB via JS Interop | Browser-native structured database persistence |
| **Testing** | xUnit | Automated testing across calculations, CSV import, and storage |
| **CI/CD** | GitHub Actions | Automated build, test, and release pipeline |
| **Hosting** | GitHub Pages | Static hosting with SPA 404 routing fallback |

---

## 💻 How to Run PowerPulse on Your Own System

### Prerequisites

Ensure you have the .NET 10 SDK installed on your system:
- **macOS / Linux / Windows:** Download from [Microsoft .NET 10 Download Page](https://dotnet.microsoft.com/download/dotnet/10.0)
- Verify installation in your terminal:
  ```bash
  dotnet --version
  # Output: 10.0.xxx
  ```

---

### Step 1: Clone the Repository

```bash
git clone https://github.com/lokeshpuma/powerpulse-cs.git
cd powerpulse-cs
```

---

### Step 2: Build the Solution

Restore dependencies and build the entire solution:
```bash
dotnet build PowerPulse.sln
```

---

### Step 3: Run Automated Tests

Run all 41 unit and infrastructure test suites:
```bash
dotnet test PowerPulse.sln
```
Expected output:
```text
Passed!  - Failed: 0, Passed: 31, Skipped: 0 - PowerPulse.Core.Tests.dll
Passed!  - Failed: 0, Passed:  8, Skipped: 0 - PowerPulse.Infrastructure.Tests.dll
Passed!  - Failed: 0, Passed:  2, Skipped: 0 - PowerPulse.Web.Tests.dll
Total: 41 Passed, 0 Failed.
```

---

### Step 4: Run the Web Application

Launch the local development server:
```bash
dotnet run --project src/PowerPulse.Web/PowerPulse.Web.csproj --launch-profile http
```

Once started, open your browser and navigate to:
👉 **`http://localhost:5216`**

> **Tip (Hot Reload):** To automatically refresh UI changes as you edit code:
> ```bash
> dotnet watch --project src/PowerPulse.Web/PowerPulse.Web.csproj --launch-profile http
> ```

---

## 📁 Repository Structure

```text
powerpulse-cs/
├── PowerPulse.sln                          # Master Solution file
├── PowerPulse.slnx                         # Modern XML Solution format
├── README.md                               # Project documentation & instructions
├── .gitattributes                          # Git binary integrity protection
├── .gitignore                              # Git exclusion rules
├── .github/
│   └── workflows/
│       └── deploy.yml                      # GitHub Actions Pages deployment workflow
│
├── src/
│   ├── PowerPulse.Core/                    # Pure C# domain library
│   │   ├── Analytics/                      # Anomaly detection & rolling MAD algorithms
│   │   ├── Calculations/                   # Tariff, appliance & carbon calculations
│   │   ├── Enums/                          # Domain enumerations
│   │   ├── Interfaces/                     # Storage & CSV service contracts
│   │   ├── Models/                         # Domain models & analytical viewmodels
│   │   └── Validation/                     # CSV mapping & validation schemas
│   │
│   ├── PowerPulse.Infrastructure/          # Infrastructure & persistence library
│   │   ├── Backup/                         # JSON backup & restore data schemas
│   │   ├── Csv/                            # CsvHelper import service & delta calculations
│   │   ├── Export/                         # CSV and print-friendly HTML export services
│   │   └── Storage/                        # IndexedDB service with in-memory fallback
│   │
│   ├── PowerPulse.ML/                      # Machine learning / time-series forecasting
│   │   └── Forecasting/                    # WASM-certified statistical forecaster
│   │
│   └── PowerPulse.Web/                     # Blazor WebAssembly Standalone Client
│       ├── Components/                     # 5-step Onboarding Wizard component
│       ├── Layout/                         # MainLayout, dark navy NavMenu
│       ├── Pages/                          # 8 core feature pages
│       └── wwwroot/                        # Bundled Chart.js, styles, JS interop, sample data
│
└── tests/
    ├── PowerPulse.Core.Tests/              # 31 domain calculation & algorithm tests
    ├── PowerPulse.Infrastructure.Tests/    # 8 CSV import, units, rollover & export tests
    └── PowerPulse.Web.Tests/               # 2 architecture & state tests
```

---

## 🚀 GitHub Pages Deployment Workflow

The project is preconfigured for zero-configuration deployment to GitHub Pages via GitHub Actions:

1. Push commits to the `main` branch.
2. The workflow at `.github/workflows/deploy.yml` automatically:
   - Restores and compiles in `Release` configuration.
   - Executes all automated unit tests.
   - Publishes standalone WebAssembly assets.
   - Applies base href pathing (`/powerpulse-cs/`) and copies `index.html` to `404.html` for SPA routing.
   - Deploys static assets to GitHub Pages.
3. In your GitHub repository settings under **Settings > Pages**, ensure the source is set to **GitHub Actions**.

---

## 📄 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
