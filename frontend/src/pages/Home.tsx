import { useApi } from "../hooks/useApi";
import { resultsApi } from "../api/results";
import { analysisApi } from "../api/analysis";
import type { ResultDto } from "../api/types";
import { DRAW_TIMES } from "../api/types";
import ResultCard from "../components/ResultCard";
import StatCard, { ChipRow } from "../components/StatCard";
import { LoadingSkeleton, ErrorState, EmptyState } from "../components/StateViews";
import { FlameIcon, SnowflakeIcon, AnalysisIcon, HistoryIcon, CalendarIcon } from "../components/icons";
import { Link } from "react-router-dom";

export default function Home() {
  const overview = useApi(() => analysisApi.overview(), []);
  // Fetch enough recent results to reliably cover all three draw times for the latest available date.
  const recent = useApi(() => resultsApi.list({ page: 1, pageSize: 15 }), []);
  const seasonal = useApi(() => analysisApi.seasonal(), []);
  const dataQuality = useApi(() => analysisApi.dataQuality(), []);

  const latestByDrawTime = new Map<string, { value: string; date: string }>();
  for (const r of recent.data?.items ?? []) {
    if (r.resultValue && !latestByDrawTime.has(r.drawTime)) {
      latestByDrawTime.set(r.drawTime, { value: r.resultValue, date: r.drawDate });
    }
  }

  // "Latest Available Results" = the most recent date that actually has data in the DB
  // (not necessarily today's calendar date), with that date's own draw-time statuses.
  const latestDate = recent.data?.items[0]?.drawDate ?? null;
  const latestDateByDrawTime = new Map(
    (recent.data?.items ?? []).filter((r) => r.drawDate === latestDate).map((r) => [r.drawTime, r])
  );
  const latestCards: ResultDto[] = latestDate
    ? DRAW_TIMES.map(
        (dt) => latestDateByDrawTime.get(dt) ?? { id: 0, drawDate: latestDate, drawTime: dt, resultValue: null, status: "Pending", lastUpdated: null }
      )
    : [];

  // Derive the "Updated" timestamp from real data — the most recent lastUpdated among that date's draws.
  const latestUpdatedIso = latestCards
    .map((r) => r.lastUpdated)
    .filter((v): v is string => !!v)
    .sort()
    .pop();
  const updatedLabel = latestUpdatedIso
    ? new Intl.DateTimeFormat("en-IN", { dateStyle: "medium", timeStyle: "short" }).format(new Date(latestUpdatedIso))
    : "—";

  // Which draw time is "active" right now, by real clock time — only meaningful (and only shown)
  // when the cards on screen are actually today's, not an older fallback date.
  const now = new Date();
  const todayLocalIso = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-${String(now.getDate()).padStart(2, "0")}`;
  const hour = now.getHours();
  const activeDrawTime = hour < 13 ? "1 PM" : hour < 18 ? "6 PM" : hour < 20 ? "8 PM" : null;
  const showActiveHighlight = latestDate === todayLocalIso;

  return (
    <div className="container">
      <h2 className="section-title" style={{ marginTop: 4 }}>Latest Available Results</h2>
      {recent.loading && <LoadingSkeleton rows={3} height={120} />}
      {recent.error && <ErrorState message={recent.error} onRetry={recent.reload} />}
      {recent.data && latestCards.length === 0 && <EmptyState message="No results yet." />}
      {latestCards.length > 0 && (
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
            <span>Updated: {updatedLabel} · {latestDate}</span>
          </div>
          <div className="grid-3" style={{ overflowX: "auto" }}>
            {latestCards.map((r) => (
              <ResultCard
                key={r.drawTime}
                result={r}
                lastAvailable={latestByDrawTime.get(r.drawTime)}
                active={showActiveHighlight && r.drawTime === activeDrawTime}
              />
            ))}
          </div>
        </div>
      )}

      {dataQuality.data && (
        <div className="grid-4">
          <SummaryStat label="Historical Draws" value={dataQuality.data.totalDraws + dataQuality.data.missingSlotCount} />
          <SummaryStat label="Available" value={dataQuality.data.totalDraws} />
          <SummaryStat label="Missing" value={dataQuality.data.missingSlotCount} />
          <SummaryStat
            label="Coverage"
            value={`${dataQuality.data.totalDraws + dataQuality.data.missingSlotCount > 0
              ? ((dataQuality.data.totalDraws / (dataQuality.data.totalDraws + dataQuality.data.missingSlotCount)) * 100).toFixed(0)
              : 0}%`}
          />
        </div>
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
          <StatCard title="Last Year, This Date" icon={<CalendarIcon size={14} />}>
            {seasonal.loading && <span style={{ fontSize: 12, color: "var(--text-muted)" }}>Loading…</span>}
            {seasonal.error && <span style={{ fontSize: 12, color: "var(--text-muted)" }}>Unable to load.</span>}
            {seasonal.data && (
              <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
                {seasonal.data.draws.map((d) => (
                  <div key={d.drawTime} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", fontSize: 12 }}>
                    <span style={{ color: "var(--text-muted)" }}>{d.drawTime}</span>
                    <span style={{ fontWeight: 700, letterSpacing: d.sameDateLastYearValue ? 1 : 0 }}>
                      {d.sameDateLastYearValue ?? <span style={{ fontSize: 11, fontWeight: 500, color: "var(--text-muted)" }}>No result found</span>}
                    </span>
                  </div>
                ))}
              </div>
            )}
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

function SummaryStat({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="card" style={{ padding: 12, textAlign: "center" }}>
      <div style={{ fontSize: 18, fontWeight: 800, color: "var(--primary)" }}>{value}</div>
      <div style={{ fontSize: 10, color: "var(--text-muted)", fontWeight: 600, marginTop: 2 }}>{label}</div>
    </div>
  );
}
