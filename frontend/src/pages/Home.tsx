import { useState } from "react";
import { useApi } from "../hooks/useApi";
import { resultsApi } from "../api/results";
import { analysisApi } from "../api/analysis";
import { predictionsApi } from "../api/predictions";
import type { PredictionHistoryDto } from "../api/types";
import ResultCard from "../components/ResultCard";
import StatCard, { ChipRow } from "../components/StatCard";
import { LoadingSkeleton, ErrorState, EmptyState } from "../components/StateViews";
import { FlameIcon, SnowflakeIcon, AnalysisIcon, HistoryIcon, CalendarIcon } from "../components/icons";
import { Link } from "react-router-dom";

export default function Home() {
  const today = useApi(() => resultsApi.today(), []);
  const overview = useApi(() => analysisApi.overview(), []);
  // Fetch enough recent results to reliably cover all three draw times for the "latest available" fallback.
  const recent = useApi(() => resultsApi.list({ page: 1, pageSize: 15 }), []);
  const latestPrediction = useApi(() => predictionsApi.history({ page: 1, pageSize: 1 }), []);

  const latestByDrawTime = new Map<string, { value: string; date: string }>();
  for (const r of recent.data?.items ?? []) {
    if (r.resultValue && !latestByDrawTime.has(r.drawTime)) {
      latestByDrawTime.set(r.drawTime, { value: r.resultValue, date: r.drawDate });
    }
  }

  // Derive the "Updated" timestamp from real data — the most recent lastUpdated among today's draws.
  const latestUpdatedIso = (today.data ?? [])
    .map((r) => r.lastUpdated)
    .filter((v): v is string => !!v)
    .sort()
    .pop();
  const updatedLabel = latestUpdatedIso
    ? new Intl.DateTimeFormat("en-IN", { dateStyle: "medium", timeStyle: "short" }).format(new Date(latestUpdatedIso))
    : "—";

  return (
    <div className="container">
      <h2 className="section-title" style={{ marginTop: 4 }}>Latest Available Results</h2>
      {today.loading && <LoadingSkeleton rows={3} height={120} />}
      {today.error && <ErrorState message={today.error} onRetry={today.reload} />}
      {today.data && (
        <div
          style={{
            background: "var(--gradient-accent)",
            borderRadius: 20,
            padding: 14,
            boxShadow: "0 6px 18px rgba(249,115,22,0.25)",
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: 6, color: "#fff", fontSize: 13, fontWeight: 600, marginBottom: 10, padding: "0 2px" }}>
            <CalendarIcon size={16} />
            <span>Updated: {updatedLabel}</span>
          </div>
          <div className="grid-3" style={{ overflowX: "auto" }}>
            {today.data.map((r) => (
              <ResultCard key={r.drawTime} result={r} lastAvailable={latestByDrawTime.get(r.drawTime)} />
            ))}
          </div>
        </div>
      )}

      <h2 className="section-title">Latest Prediction</h2>
      {latestPrediction.loading && <LoadingSkeleton rows={1} height={140} />}
      {latestPrediction.error && <ErrorState message={latestPrediction.error} onRetry={latestPrediction.reload} />}
      {latestPrediction.data && latestPrediction.data.items.length === 0 && <EmptyState message="No predictions saved yet." />}
      {latestPrediction.data && latestPrediction.data.items.length > 0 && (
        <LatestPredictionCard prediction={latestPrediction.data.items[0]} />
      )}

      <h2 className="section-title">Quick Analysis</h2>
      {overview.loading && <LoadingSkeleton rows={2} height={90} />}
      {overview.error && <ErrorState message={overview.error} onRetry={overview.reload} />}
      {overview.data && (
        <div className="grid-2">
          <StatCard title="Hot Numbers" icon={<FlameIcon size={14} />} to="/analysis?tab=hot">
            <ChipRow values={overview.data.frequency.hotNumbers.slice(0, 6).map((f) => f.value)} />
          </StatCard>
          <StatCard title="Cold Numbers" icon={<SnowflakeIcon size={14} />} to="/analysis?tab=cold">
            <ChipRow values={overview.data.frequency.coldNumbers.slice(0, 6).map((f) => f.value)} />
          </StatCard>
          <StatCard title="Last 2 Digits" icon={<AnalysisIcon size={14} />} to="/analysis?tab=last2">
            <ChipRow values={overview.data.frequency.last2DigitFrequency.slice(0, 6).map((f) => f.value)} />
          </StatCard>
          <StatCard title="Last 3 Digits" icon={<AnalysisIcon size={14} />} to="/analysis?tab=last3">
            <ChipRow values={overview.data.frequency.last3DigitFrequency.slice(0, 6).map((f) => f.value)} />
          </StatCard>
          <StatCard title="Recent Repeats" icon={<HistoryIcon size={14} />} to="/analysis?tab=overview">
            <ChipRow values={overview.data.patterns.recentRepeats.slice(0, 6).map((r) => r.value)} />
          </StatCard>
          <StatCard title="Digit Frequency" icon={<AnalysisIcon size={14} />} to="/analysis?tab=frequency">
            <ChipRow values={overview.data.frequency.lastDigitFrequency.slice(0, 6).map((f) => f.value)} />
          </StatCard>
          <StatCard title="Model Evaluation" icon={<HistoryIcon size={14} />} to="/analysis?tab=modelComparison">
            <span style={{ fontSize: 12, color: "var(--text-muted)" }}>Current model vs frequency, recency &amp; random baselines →</span>
          </StatCard>
        </div>
      )}

      <h2 className="section-title">Recent Results</h2>
      {recent.loading && <LoadingSkeleton rows={4} height={44} />}
      {recent.error && <ErrorState message={recent.error} onRetry={recent.reload} />}
      {recent.data && recent.data.items.length === 0 && <EmptyState message="No results yet." />}
      {recent.data && recent.data.items.length > 0 && (
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          {recent.data.items.slice(0, 5).map((r, i) => (
            <div
              key={r.id}
              style={{
                display: "flex",
                justifyContent: "space-between",
                padding: "10px 14px",
                borderTop: i === 0 ? "none" : "1px solid var(--border)",
                fontSize: 13,
              }}
            >
              <span style={{ color: "var(--text-muted)" }}>{r.drawDate} · {r.drawTime}</span>
              <span style={{ fontWeight: 700 }}>{r.resultValue}</span>
            </div>
          ))}
          <div style={{ padding: "8px 14px", borderTop: "1px solid var(--border)" }}>
            <Link to="/results" style={{ fontSize: 12, color: "var(--primary)", fontWeight: 600 }}>View all results →</Link>
          </div>
        </div>
      )}

      <p className="disclaimer">Statistical analysis only. Past results do not guarantee future results.</p>
    </div>
  );
}

function LatestPredictionCard({ prediction: p }: { prediction: PredictionHistoryDto }) {
  const [expanded, setExpanded] = useState(false);
  const top = p.candidates.slice(0, 10);
  const best = top[0];

  return (
    <div className="card" style={{ display: "flex", flexDirection: "column", gap: 8 }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", flexWrap: "wrap", gap: 6 }}>
        <span style={{ fontSize: 13, fontWeight: 600 }}>{p.drawDate} · {p.drawTime}</span>
        <span className={`badge ${p.isEvaluated ? "badge-success" : "badge-warning"}`}>{p.isEvaluated ? "Evaluated" : "Pending"}</span>
      </div>

      {best && (
        <div style={{ fontSize: 12, color: "var(--text-muted)" }}>
          Top candidate: <b style={{ color: "var(--text)" }}>{best.value}</b> · Model Score <b style={{ color: "var(--text)" }}>{best.modelScore}</b>
          {" · "}Historical Frequency <b style={{ color: "var(--text)" }}>{best.historicalFrequency}</b> · Recent Frequency <b style={{ color: "var(--text)" }}>{best.recentFrequency}</b>
          <div style={{ marginTop: 2 }}>{best.reason}</div>
        </div>
      )}

      {p.isEvaluated && (
        <div style={{ fontSize: 12, color: "var(--text-muted)" }}>
          Actual result: <b style={{ color: "var(--text)" }}>{p.actualResult}</b>
          <div style={{ display: "flex", gap: 6, marginTop: 4, flexWrap: "wrap" }}>
            <span className={`badge ${p.exactMatch ? "badge-success" : "badge-muted"}`}>Exact: {p.exactMatch === null ? "N/A" : p.exactMatch ? "Match" : "No match"}</span>
            <span className={`badge ${p.last3Match ? "badge-success" : "badge-muted"}`}>Last-3: {p.last3Match === null ? "N/A" : p.last3Match ? "Match" : "No match"}</span>
            <span className={`badge ${p.last2Match ? "badge-success" : "badge-muted"}`}>Last-2: {p.last2Match === null ? "N/A" : p.last2Match ? "Match" : "No match"}</span>
          </div>
        </div>
      )}

      {top.length > 0 && (
        <button
          className="btn btn-outline"
          style={{ padding: "4px 10px", minHeight: 26, fontSize: 11, alignSelf: "flex-start" }}
          onClick={() => setExpanded(!expanded)}
        >
          {expanded ? "Hide candidates" : `Show all ${top.length} candidates`}
        </button>
      )}
      {expanded && (
        <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
          {top.map((c) => (
            <div key={c.value} style={{ padding: "6px 8px", background: "var(--bg)", borderRadius: 8 }}>
              <div style={{ display: "flex", justifyContent: "space-between" }}>
                <span style={{ fontWeight: 700 }}>{c.value}</span>
                <span style={{ fontSize: 12, color: "var(--text-muted)" }}>Model Score <b style={{ color: "var(--text)" }}>{c.modelScore}</b></span>
              </div>
              <div style={{ fontSize: 11, color: "var(--text-muted)" }}>
                Historical Frequency <b style={{ color: "var(--text)" }}>{c.historicalFrequency}</b> · Recent Frequency <b style={{ color: "var(--text)" }}>{c.recentFrequency}</b>
              </div>
              <div style={{ fontSize: 11, color: "var(--text-muted)" }}>{c.reason}</div>
            </div>
          ))}
        </div>
      )}

      <Link to="/analysis/history" style={{ fontSize: 12, color: "var(--primary)", fontWeight: 600 }}>View Prediction History →</Link>
    </div>
  );
}
