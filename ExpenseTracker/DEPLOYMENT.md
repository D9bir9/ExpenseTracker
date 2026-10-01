# Railway deployment

The application supports SQLite for local development and PostgreSQL for deployment. Railway can build and run the app with Railpack; a Dockerfile is not required.

## Configure Railway

1. Create a Railway project and add a PostgreSQL service.
2. Add the application as a service from this repository and set its root directory to `ExpenseTracker`.
3. Add these application service variables:
   - `DatabaseProvider=PostgreSql`
   - `DATABASE_URL=${{Postgres.DATABASE_URL}}` (replace `Postgres` with the name of the PostgreSQL service if it differs)
   - `ASPNETCORE_ENVIRONMENT=Production`
4. Deploy. The app listens on Railway's `PORT`, checks `/health`, and applies the PostgreSQL migrations on startup.

The application also accepts a standard Npgsql connection string in `ConnectionStrings__DefaultConnection` instead of `DATABASE_URL`. PostgreSQL URL connections are parsed and configured to require TLS.

## Migrations and existing data

SQLite and PostgreSQL use separate EF Core migration sets. Create PostgreSQL schema changes against `PostgreSqlMigrationsDbContext`, not `ApplicationDbContext`:

```sh
dotnet ef migrations add <MigrationName> \
  --context PostgreSqlMigrationsDbContext \
  --output-dir Migrations/PostgreSql
```

Run that command with `DatabaseProvider=PostgreSql` and a PostgreSQL design-time connection string configured. Continue using `ApplicationDbContext` for SQLite migrations.

The first PostgreSQL deployment creates a separate, empty database; it does not copy users or financial records from `Data/app.db`. Plan and verify a data export/import separately before directing users to the new deployment. Back up the PostgreSQL service regularly.
