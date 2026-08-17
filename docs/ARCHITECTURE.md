# Architecture

```
frontend  →  ASP.NET Core API  →  SQL Server
                    ↑
              Sambad API (backend-only; credentials never reach the frontend)
```

## Scoring model

`FinalScore = FrequencyScore + RecencyScore + DigitScore + RepeatScore + PatternScore` (weighted, normalized 0–100). Weights are configurable via the `ScoringWeights` config section. See `backend/Services/Analysis/CandidateScoringService.cs`.

**Model Score is a statistical weighting, not a probability of winning.** Lottery draws are random; backtesting results do not guarantee future performance.

## Caching

- `AnalysisSnapshot` table caches the `/api/analysis/overview` response for 6 hours to avoid recomputing on every request.
- Results/history endpoints use server-side pagination and `AsNoTracking()` queries.
