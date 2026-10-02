# ExpenseTracker

ExpenseTracker is a personal finance dashboard built with ASP.NET Core MVC and Entity Framework Core. It helps users manage income and expenses, organize categories, set monthly budget targets, and monitor spending trends through a clean, modern dashboard.

## Overview

This application is designed for individuals who want a simple and efficient way to track their financial activity without the complexity of enterprise accounting systems. Users can:

- Create and manage personal spending categories
- Record income and expense transactions
- Set monthly budget goals by category
- Review current balance and recent activity
- Monitor spending trends using dashboard charts and summaries
- Switch between regional currency and culture formatting

## Key Features

- Secure authentication with ASP.NET Core Identity
- Per-user data isolation for personal finance records
- Expense and income tracking with detailed notes and dates
- Category management with icon labeling and monthly budget limits
- Budget overview and spending progress monitoring
- Dashboard analytics for income, expenses, and balance
- Currency conversion support for localized reporting
- SQLite support for local development and PostgreSQL support for deployment
- Health endpoint for deployment checks (`/health`)

## Tech Stack

- ASP.NET Core MVC
- C# / .NET 10
- Entity Framework Core
- SQLite
- PostgreSQL (deployment-ready)
- ASP.NET Core Identity
- Redis cache support
- Syncfusion EJ2 components for dashboard visuals

## Project Structure

```text
ExpenseTracker/
├── Controllers/
│   ├── AccountController.cs
│   ├── BudgetController.cs
│   ├── CategoryController.cs
│   ├── DashboardController.cs
│   ├── HomeController.cs
│   ├── TransactionController.cs
│   └── UserScopedController.cs
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── DbInitializer.cs
│   ├── PostgreSqlMigrationsDbContext.cs
│   ├── Repository/
│   └── app.db
├── Helpers/
│   └── CurrencyDisplayHelper.cs
├── Migrations/
├── Models/
│   ├── Account/
│   ├── ApplicationUser.cs
│   ├── Category.cs
│   ├── ErrorViewModel.cs
│   └── Transaction.cs
├── Services/
│   ├── CurrencyConversionService.cs
│   └── ICurrencyConversionService.cs
├── Views/
├── wwwroot/
├── appsettings.json
├── appsettings.Development.json
├── DEPLOYMENT.md
├── ExpenseTracker.csproj
├── Program.cs
├── railway.json
├── .gitignore
└── README.md
```

## Screenshots

The following screenshots showcase the application dashboard and core management screens.

> Note: Add the files below to the repository under `docs/screenshots/` to render them correctly on GitHub.

<img width="1920" height="1080" alt="Screenshot_20261002_231732" src="https://github.com/user-attachments/assets/5f13ee62-a236-4167-9ed7-dc5bf7e261c7" />


<img width="786" height="967" alt="Screenshot_20261002_231847" src="https://github.com/user-attachments/assets/11bb116c-6ba1-45df-93b4-d9c3997c47bc" />
<img width="1920" height="1080" alt="Screenshot_20261002_232040" src="https://github.com/user-attachments/assets/5613305f-5fa3-41e7-b102-5d8d380cc3a4" />
<img width="1920" height="1080" alt="Screenshot_20261002_232137" src="https://github.com/user-attachments/assets/7d111df5-05b3-4f1c-8d28-6ef0c725ea4b" />
<img width="1920" height="1080" alt="Screenshot_20261002_232331" src="https://github.com/user-attachments/assets/30b01d88-1c97-4b32-ab95-185bf8c86714" />

<img width="624" height="967" alt="Screenshot_20261002_232246-1" src="https://github.com/user-attachments/assets/6152ebcb-fde9-4e26-9413-321ceca26473" />


## Prerequisites

Before running the app, make sure you have:

- .NET 10 SDK
- SQLite or PostgreSQL available for your environment
- Optional: Redis if you plan to use the configured cache support

## Getting Started

1. Clone the repository:

```bash
git clone https://github.com/D9bir9/ExpenseTracker.git
cd ExpenseTracker
```

2. Restore NuGet packages:

```bash
dotnet restore
```

3. Run the application:

```bash
dotnet run
```

By default, the app uses SQLite for local development. Production deployment can be configured to use PostgreSQL via environment variables or connection strings.

## Configuration

The app reads settings from `appsettings.json`:

```json
{
  "DatabaseProvider": "Sqlite",
  "ConnectionStrings": {
    "DefaultConnection": "DataSource=Data/app.db;Cache=Shared",
    "Redis": "localhost:6379"
  }
}
```

For PostgreSQL deployments, set:

```bash
DatabaseProvider=PostgreSql
DATABASE_URL=postgresql://user:password@host:5432/database
```

See `DEPLOYMENT.md` for Railway deployment guidance.

## Authentication and Security

ExpenseTracker uses ASP.NET Core Identity for user registration, sign-in, and account security. Each user is scoped to their own data, ensuring that transactions and categories are isolated by account.

## Dashboard Functionality

The dashboard includes:

- Total income, total expense, and balance cards
- Monthly budget summaries
- Category-based spending insights
- Recent transaction history
- Budget vs. spending comparison charts
- Visual progress tracking for category limits

## Deployment

This project is configured for deployment on platforms such as Railway. The repository includes a `railway.json` and deployment documentation in `DEPLOYMENT.md`.

## License

This project does not currently include a license file. If you plan to publish or distribute the project publicly, consider adding an open-source license such as MIT.

## Contributing

Contributions are welcome. To contribute:

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Open a pull request with a clear description

## Contact

For questions or support, visit the repository owner on GitHub: https://github.com/D9bir9
