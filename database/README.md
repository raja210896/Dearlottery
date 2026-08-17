# Database

EF Core manages the schema — see `backend/Data/AppDbContext.cs` and `backend/Data/Migrations/`.

## Apply migrations

```bash
cd backend
dotnet ef database update
```

## Historical data import

Sambad sync only pulls permitted current-day results (see `backend/Services/Sambad/`). If you have permitted historical data as CSV/JSON, add an import endpoint under `backend/Controllers/` that maps rows into `LotteryResult` — do not scrape or copy restricted historical data.
