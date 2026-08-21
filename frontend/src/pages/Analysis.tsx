import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useApi } from "../hooks/useApi";
import { analysisApi } from "../api/analysis";
import { predictionsApi } from "../api/predictions";
import { LoadingSkeleton, ErrorState, EmptyState } from "../components/StateViews";
import FrequencyTable from "../components/FrequencyTable";
import SimpleBarChart from "../components/SimpleBarChart";
import { ChipRow } from "../components/StatCard";
import { DRAW_TIMES } from "../api/types";
import { ApiError } from "../api/client";

const TABS = [
  { key: "overview", label: "Overview" },
  { key: "frequency", label: "Frequency" },
  { key: "hot", label: "Hot Numbers" },
  { key: "cold", label: "Cold Numbers" },
  { key: "last2", label: "Last 2 Digits" },
  { key: "last3", label: "Last 3 Digits" },
  { key: "digits", label: "Digits" },
  { key: "candidates", label: "Candidates" },
  { key: "backtest", label: "Backtesting" },
  { key: "performance", label: "Performance" },
  { key: "modelComparison", label: "Model Comparison" },
] as const;
type TabKey = (typeof TABS)[number]["key"];

export default function Analysis() {
  const [params, setParams] = useSearchParams();
  const tab = (params.get("tab") as TabKey) || "overview";
  const [drawTime, setDrawTime] = useState("");

  const setTab = (t: TabKey) => setParams((p) => { p.set("tab", t); return p; });

  return (
    <div className="container">
      <h1 className="section-title" style={{ marginTop: 4 }}>Analysis</h1>

      <div className="card" style={{ marginBottom: 12 }}>
        <select value={drawTime} onChange={(e) => setDrawTime(e.target.value)}>
          <option value="">All draws</option>
          {DRAW_TIMES.map((d) => <option key={d} value={d}>{d}</option>)}
        </select>
      </div>

      <div style={{ display: "flex", gap: 6, overflowX: "auto", paddingBottom: 8, marginBottom: 8 }}>
        {TABS.map((t) => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={`pill-btn${tab === t.key ? " active" : ""}`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === "overview" && <OverviewTab drawTime={drawTime} />}
      {tab === "frequency" && <FrequencyTab drawTime={drawTime} />}
      {tab === "hot" && <HotColdTab drawTime={drawTime} mode="hot" />}
      {tab === "cold" && <HotColdTab drawTime={drawTime} mode="cold" />}
      {tab === "last2" && <DigitsTab drawTime={drawTime} which="last2" />}
      {tab === "last3" && <DigitsTab drawTime={drawTime} which="last3" />}
      {tab === "digits" && <DigitAnalysisTab drawTime={drawTime} />}
      {tab === "candidates" && <CandidatesTab drawTime={drawTime} />}
      {tab === "backtest" && <BacktestTab drawTime={drawTime} />}
      {tab === "performance" && <PerformanceTab drawTime={drawTime} />}
      {tab === "modelComparison" && <ModelComparisonTab />}

      <p className="disclaimer">Statistical analysis only. Past results do not guarantee future results.</p>
    </div>
  );
}

function OverviewTab({ drawTime }: { drawTime: string }) {
  const overview = useApi(() => analysisApi.overview(drawTime || undefined), [drawTime]);
  if (overview.loading) return <LoadingSkeleton rows={3} height={100} />;
  if (overview.error) return <ErrorState message={overview.error} onRetry={overview.reload} />;
  if (!overview.data || overview.data.frequency.sampleSize === 0) return <EmptyState message="No results yet to analyze." />;

  const { frequency, patterns } = overview.data;
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
      <div className="card">
        <div className="section-title" style={{ marginTop: 0 }}>Hot Numbers</div>
        <ChipRow values={frequency.hotNumbers.slice(0, 8).map((f) => f.value)} />
      </div>
      <div className="card">
        <div className="section-title" style={{ marginTop: 0 }}>Cold Numbers</div>
        <ChipRow values={frequency.coldNumbers.slice(0, 8).map((f) => f.value)} />
      </div>
      <div className="card">
        <div className="section-title" style={{ marginTop: 0 }}>Odd / Even Split</div>
        <SimpleBarChart bars={[
          { label: "Odd", value: patterns.oddCount / Math.max(1, patterns.oddCount + patterns.evenCount) },
          { label: "Even", value: patterns.evenCount / Math.max(1, patterns.oddCount + patterns.evenCount) },
        ]} />
      </div>
      <div className="card">
        <div className="section-title" style={{ marginTop: 0 }}>Sample size: {frequency.sampleSize} draws</div>
      </div>
    </div>
  );
}

function FrequencyTab({ drawTime }: { drawTime: string }) {
  const freq = useApi(() => analysisApi.frequency({ drawTime: drawTime || undefined }), [drawTime]);
  if (freq.loading) return <LoadingSkeleton rows={2} height={200} />;
  if (freq.error) return <ErrorState message={freq.error} onRetry={freq.reload} />;
  if (!freq.data || freq.data.sampleSize === 0) return <EmptyState message="No results yet to analyze." />;

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
      <div className="card">
        <div className="section-title" style={{ marginTop: 0 }}>Last Digit Frequency</div>
        <FrequencyTable entries={freq.data.lastDigitFrequency} limit={10} />
      </div>
      <div className="card">
        <div className="section-title" style={{ marginTop: 0 }}>Full Number Frequency</div>
        <FrequencyTable entries={freq.data.fullNumberFrequency} limit={15} />
      </div>
    </div>
  );
}

function HotColdTab({ drawTime, mode }: { drawTime: string; mode: "hot" | "cold" }) {
  const freq = useApi(() => analysisApi.frequency({ drawTime: drawTime || undefined }), [drawTime]);
  if (freq.loading) return <LoadingSkeleton rows={2} height={200} />;
  if (freq.error) return <ErrorState message={freq.error} onRetry={freq.reload} />;
  if (!freq.data || freq.data.sampleSize === 0) return <EmptyState message="No results yet to analyze." />;

  const entries = mode === "hot" ? freq.data.hotNumbers : freq.data.coldNumbers;
  return (
    <div className="card">
      <div className="section-title" style={{ marginTop: 0 }}>{mode === "hot" ? "Hot Numbers" : "Cold Numbers"} (last 2 digits)</div>
      <FrequencyTable entries={entries} limit={10} />
    </div>
  );
}

function DigitsTab({ drawTime, which }: { drawTime: string; which: "last2" | "last3" }) {
  const freq = useApi(() => analysisApi.frequency({ drawTime: drawTime || undefined }), [drawTime]);
  if (freq.loading) return <LoadingSkeleton rows={2} height={200} />;
  if (freq.error) return <ErrorState message={freq.error} onRetry={freq.reload} />;
  if (!freq.data || freq.data.sampleSize === 0) return <EmptyState message="No results yet to analyze." />;

  const entries = which === "last2" ? freq.data.last2DigitFrequency : freq.data.last3DigitFrequency;
  return (
    <div className="card">
      <div className="section-title" style={{ marginTop: 0 }}>{which === "last2" ? "Last 2 Digits" : "Last 3 Digits"} Frequency</div>
      <FrequencyTable entries={entries} limit={20} />
    </div>
  );
}

function DigitAnalysisTab({ drawTime }: { drawTime: string }) {
  const digits = useApi(() => analysisApi.digits({ drawTime: drawTime || undefined }), [drawTime]);
  if (digits.loading) return <LoadingSkeleton rows={3} height={100} />;
  if (digits.error) return <ErrorState message={digits.error} onRetry={digits.reload} />;
  if (!digits.data || digits.data.sampleSize === 0) return <EmptyState message="No results yet to analyze." />;

  const d = digits.data;
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
      <div className="card">
        <div className="section-title" style={{ marginTop: 0 }}>Digit Frequency (0-9)</div>
        <FrequencyTable entries={d.digitFrequency} limit={10} />
      </div>
      <div className="grid-2">
        <div className="card">
          <div className="section-title" style={{ marginTop: 0 }}>Hot Digits</div>
          <ChipRow values={d.hotDigits.map((f) => f.value)} />
        </div>
        <div className="card">
          <div className="section-title" style={{ marginTop: 0 }}>Cold Digits</div>
          <ChipRow values={d.coldDigits.map((f) => f.value)} />
        </div>
      </div>
      <div className="card">
        <div className="section-title" style={{ marginTop: 0 }}>Position Frequency</div>
        <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
          {d.positionFrequency.map((p) => (
            <div key={p.position}>
              <div style={{ fontSize: 12, color: "var(--text-muted)", marginBottom: 4 }}>Position {p.position}</div>
              <ChipRow values={p.digits.slice(0, 4).map((f) => f.value)} />
            </div>
          ))}
        </div>
      </div>
      <div className="card">
        <div className="section-title" style={{ marginTop: 0 }}>Digit Pair Frequency (transitions)</div>
        <FrequencyTable entries={d.digitPairFrequency} limit={10} />
      </div>
      <div className="card">
        <div className="section-title" style={{ marginTop: 0 }}>Recent vs Historical (last 2 digits)</div>
        <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
          {d.recentVsHistorical.slice(0, 10).map((r) => (
            <div key={r.value} style={{ display: "flex", justifyContent: "space-between", fontSize: 13 }}>
              <span style={{ fontWeight: 700 }}>{r.value}</span>
              <span style={{ color: "var(--text-muted)" }}>Recent {r.recentCount} · Historical {r.historicalCount}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function CandidatesTab({ drawTime }: { drawTime: string }) {
  const [digitLength, setDigitLength] = useState(2);
  const [count, setCount] = useState(10);
  const [drawDate, setDrawDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [saveStatus, setSaveStatus] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const candidates = useApi(
    () => analysisApi.candidates({ draw: drawTime || undefined, digitLength, count }),
    [drawTime, digitLength, count]
  );

  async function handleSavePrediction() {
    if (!drawTime) return;
    setSaving(true);
    setSaveStatus(null);
    try {
      await predictionsApi.save({ drawDate, drawTime, digitLength, count });
      setSaveStatus("Prediction snapshot saved.");
    } catch (err) {
      setSaveStatus(err instanceof ApiError ? err.message : "Failed to save prediction.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
      <div className="card" style={{ display: "flex", gap: 10, flexWrap: "wrap" }}>
        <label style={{ fontSize: 13, display: "flex", alignItems: "center", gap: 6 }}>
          Digit length
          <select value={digitLength} onChange={(e) => setDigitLength(Number(e.target.value))}>
            <option value={1}>1</option>
            <option value={2}>2</option>
            <option value={3}>3</option>
            <option value={5}>5 (exact)</option>
          </select>
        </label>
        <label style={{ fontSize: 13, display: "flex", alignItems: "center", gap: 6 }}>
          Candidates
          <select value={count} onChange={(e) => setCount(Number(e.target.value))}>
            <option value={5}>5</option>
            <option value={10}>10</option>
            <option value={20}>20</option>
          </select>
        </label>
      </div>

      {candidates.loading && <LoadingSkeleton rows={4} height={40} />}
      {candidates.error && <ErrorState message={candidates.error} onRetry={candidates.reload} />}
      {candidates.data && candidates.data.candidates.length === 0 && <EmptyState message="Not enough historical data yet." />}
      {candidates.data && candidates.data.candidates.length > 0 && (
        <div className="card" style={{ padding: 0 }}>
          <div style={{ padding: "10px 14px", fontSize: 12, color: "var(--text-muted)", borderBottom: "1px solid var(--border)" }}>
            Draw Time: <b style={{ color: "var(--text)" }}>{candidates.data.drawTime}</b>
          </div>
          {candidates.data.candidates.map((c, i) => (
            <div key={c.value} style={{ padding: "10px 14px", borderTop: i === 0 ? "none" : "1px solid var(--border)" }}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                <span style={{ fontWeight: 700, fontSize: 16 }}>{c.value}</span>
                <span style={{ fontSize: 13, color: "var(--text-muted)" }}>Model Score <b style={{ color: "var(--text)" }}>{c.modelScore}</b></span>
              </div>
              <div style={{ fontSize: 12, color: "var(--text-muted)", marginTop: 2 }}>
                Historical Frequency <b style={{ color: "var(--text)" }}>{c.historicalFrequency}</b> · Recent Frequency <b style={{ color: "var(--text)" }}>{c.recentFrequency}</b>
              </div>
              <div style={{ fontSize: 12, color: "var(--text-muted)", marginTop: 2 }}>{c.reason}</div>
            </div>
          ))}
        </div>
      )}

      <div className="card" style={{ display: "flex", gap: 10, flexWrap: "wrap", alignItems: "center" }}>
        <label style={{ fontSize: 13, display: "flex", alignItems: "center", gap: 6 }}>
          Draw date
          <input type="date" value={drawDate} onChange={(e) => setDrawDate(e.target.value)} />
        </label>
        <button className="btn btn-primary" disabled={!drawTime || saving} onClick={handleSavePrediction}>
          {saving ? "Saving..." : "Save Prediction"}
        </button>
        {!drawTime && <span style={{ fontSize: 12, color: "var(--text-muted)" }}>Select a specific draw above to save a prediction.</span>}
        {saveStatus && <span style={{ fontSize: 12, color: "var(--text-muted)" }}>{saveStatus}</span>}
        <Link to="/analysis/history" style={{ fontSize: 12, color: "var(--primary)", fontWeight: 600 }}>View Prediction History →</Link>
      </div>
    </div>
  );
}

function PerformanceTab({ drawTime }: { drawTime: string }) {
  const perf = useApi(() => predictionsApi.performance(drawTime || undefined), [drawTime]);
  if (perf.loading) return <LoadingSkeleton rows={2} height={100} />;
  if (perf.error) return <ErrorState message={perf.error} onRetry={perf.reload} />;
  if (!perf.data || perf.data.totalPredictions === 0) return <EmptyState message="No predictions saved yet." />;

  const d = perf.data;
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
      <div className="card">
        <div className="section-title" style={{ marginTop: 0 }}>Prediction Performance</div>
        <div className="grid-2" style={{ marginTop: 8 }}>
          <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Total Predictions</div><div style={{ fontSize: 20, fontWeight: 700 }}>{d.totalPredictions}</div></div>
          <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Evaluated</div><div style={{ fontSize: 20, fontWeight: 700 }}>{d.evaluatedPredictions}</div></div>
          <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Matches</div><div style={{ fontSize: 20, fontWeight: 700 }}>{d.matches}</div></div>
          <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Historical Match Rate</div><div style={{ fontSize: 20, fontWeight: 700 }}>{(d.matchRate * 100).toFixed(1)}%</div></div>
        </div>
      </div>

      {d.evaluatedPredictions > 0 && (
        <div className="card">
          <div className="section-title" style={{ marginTop: 0 }}>Historical Comparison Only</div>
          <SimpleBarChart bars={[
            { label: "Model", value: d.matchRate },
            { label: "Random", value: d.randomBaselineRate, color: "var(--text-muted)" },
          ]} />
        </div>
      )}

      {d.recentPerformance.length > 0 && (
        <div className="card">
          <div className="section-title" style={{ marginTop: 0 }}>Recent Performance</div>
          <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
            {d.recentPerformance.map((r, i) => (
              <span key={i} className={`badge ${r.matchFound ? "badge-success" : "badge-muted"}`}>
                {r.drawDate} {r.drawTime} {r.matchFound ? "Match" : "No match"}
              </span>
            ))}
          </div>
        </div>
      )}

      <p className="disclaimer" style={{ padding: 0 }}>{d.disclaimer}</p>
      <Link to="/analysis/history" style={{ fontSize: 12, color: "var(--primary)", fontWeight: 600 }}>View Prediction History →</Link>
    </div>
  );
}

const BACKTEST_PRESETS = [30, 60, 100];

function DataQualityCard({ drawTime }: { drawTime: string }) {
  const dq = useApi(() => analysisApi.dataQuality(drawTime || undefined), [drawTime]);
  if (dq.loading) return <LoadingSkeleton rows={1} height={70} />;
  if (dq.error) return <ErrorState message={dq.error} onRetry={dq.reload} />;
  if (!dq.data || dq.data.totalDraws === 0) return <EmptyState message="No historical data yet." />;

  const d = dq.data;
  return (
    <div className="card">
      <div className="section-title" style={{ marginTop: 0 }}>Data Quality</div>
      <div className="grid-2">
        <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Total Draws</div><div style={{ fontSize: 16, fontWeight: 700 }}>{d.totalDraws}</div></div>
        <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Date Range</div><div style={{ fontSize: 13, fontWeight: 600 }}>{d.earliestDate} → {d.latestDate}</div></div>
        <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Missing Slots</div><div style={{ fontSize: 16, fontWeight: 700 }}>{d.missingSlotCount}</div></div>
        <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Duplicates</div><div style={{ fontSize: 16, fontWeight: 700 }}>{d.duplicateCount}</div></div>
      </div>
      <div style={{ marginTop: 8, display: "flex", gap: 6, flexWrap: "wrap" }}>
        {d.countsByDrawTime.map((c) => (
          <span key={c.drawTime} className="badge badge-muted">{c.drawTime}: {c.count}</span>
        ))}
      </div>
    </div>
  );
}

function BacktestTab({ drawTime }: { drawTime: string }) {
  const [drawCount, setDrawCount] = useState(30);
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [digitLength, setDigitLength] = useState(2);
  const [candidateCount, setCandidateCount] = useState(10);
  const useCustomRange = !!(from && to);

  const backtest = useApi(
    () => analysisApi.backtest({
      draw: drawTime || undefined, digitLength, candidateCount,
      drawCount: useCustomRange ? 100000 : drawCount,
      from: useCustomRange ? from : undefined,
      to: useCustomRange ? to : undefined,
    }),
    [drawTime, drawCount, digitLength, candidateCount, from, to]
  );
  const multi = useApi(
    () => analysisApi.backtestMulti({
      draw: drawTime || undefined, candidateCount,
      drawCount: useCustomRange ? 100000 : drawCount,
      from: useCustomRange ? from : undefined,
      to: useCustomRange ? to : undefined,
    }),
    [drawTime, drawCount, candidateCount, from, to]
  );

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
      <DataQualityCard drawTime={drawTime} />

      <div className="card" style={{ display: "flex", gap: 8, flexWrap: "wrap", alignItems: "center" }}>
        {BACKTEST_PRESETS.map((n) => (
          <button
            key={n}
            className={`pill-btn${!useCustomRange && drawCount === n ? " active" : ""}`}
            onClick={() => { setDrawCount(n); setFrom(""); setTo(""); }}
          >
            {n} draws
          </button>
        ))}
      </div>

      <div className="card" style={{ display: "flex", gap: 10, flexWrap: "wrap", alignItems: "center" }}>
        <label style={{ fontSize: 13, display: "flex", alignItems: "center", gap: 6 }}>
          Custom range
          <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
          <input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
        </label>
        <label style={{ fontSize: 13, display: "flex", alignItems: "center", gap: 6 }}>
          Digit length
          <select value={digitLength} onChange={(e) => setDigitLength(Number(e.target.value))}>
            <option value={1}>1</option>
            <option value={2}>2</option>
            <option value={3}>3</option>
            <option value={5}>5 (exact)</option>
          </select>
        </label>
        <label style={{ fontSize: 13, display: "flex", alignItems: "center", gap: 6 }}>
          Top N candidates
          <select value={candidateCount} onChange={(e) => setCandidateCount(Number(e.target.value))}>
            <option value={5}>5</option>
            <option value={10}>10</option>
            <option value={20}>20</option>
          </select>
        </label>
      </div>

      {backtest.loading && <LoadingSkeleton rows={2} height={140} />}
      {backtest.error && <ErrorState message={backtest.error} onRetry={backtest.reload} />}
      {backtest.data && backtest.data.drawsTested === 0 && <EmptyState message="Not enough historical data yet." />}
      {backtest.data && backtest.data.drawsTested > 0 && (
        <>
          <div className="card">
            <div className="section-title" style={{ marginTop: 0 }}>Historical Model Performance</div>
            <div className="grid-2">
              <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Total Tested</div><div style={{ fontSize: 18, fontWeight: 700 }}>{backtest.data.totalTested}</div></div>
              <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Evaluated</div><div style={{ fontSize: 18, fontWeight: 700 }}>{backtest.data.drawsTested}</div></div>
              <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Matches</div><div style={{ fontSize: 18, fontWeight: 700 }}>{backtest.data.hits}</div></div>
              <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Historical Match Rate</div><div style={{ fontSize: 18, fontWeight: 700 }}>{(backtest.data.modelHitRate * 100).toFixed(1)}%</div></div>
            </div>
            <p style={{ fontSize: 12, color: "var(--text-muted)", margin: "8px 0 0" }}>
              Historical Match Rate: {(backtest.data.modelHitRate * 100).toFixed(1)}% ({backtest.data.hits} matches / {backtest.data.drawsTested} tested)
            </p>
          </div>

          <div className="card">
            <div className="section-title" style={{ marginTop: 0 }}>Model vs Random Baseline</div>
            <SimpleBarChart bars={[
              { label: "Model", value: backtest.data.modelHitRate },
              { label: "Random", value: backtest.data.randomBaselineRate, color: "var(--text-muted)" },
            ]} />
            <p style={{ fontSize: 12, color: "var(--text-muted)", margin: "6px 0 0" }}>
              Difference: {(backtest.data.modelVsRandomDifference * 100).toFixed(1)} pts
            </p>
          </div>

          <div className="card">
            <div className="section-title" style={{ marginTop: 0 }}>Top-N Match Rates</div>
            <div className="grid-3">
              <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Top 1</div><div style={{ fontSize: 16, fontWeight: 700 }}>{(backtest.data.top1MatchRate * 100).toFixed(1)}%</div></div>
              <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Top 5</div><div style={{ fontSize: 16, fontWeight: 700 }}>{(backtest.data.top5MatchRate * 100).toFixed(1)}%</div></div>
              <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Top 10</div><div style={{ fontSize: 16, fontWeight: 700 }}>{(backtest.data.top10MatchRate * 100).toFixed(1)}%</div></div>
            </div>
          </div>

          {multi.data && (
            <div className="card">
              <div className="section-title" style={{ marginTop: 0 }}>Exact vs Last-2 vs Last-3 (Candidate Hit Rate)</div>
              <div className="grid-3">
                <div>
                  <div style={{ fontSize: 12, color: "var(--text-muted)" }}>Exact Matches</div>
                  <div style={{ fontSize: 16, fontWeight: 700 }}>{(multi.data.exact.modelHitRate * 100).toFixed(1)}%</div>
                  <div style={{ fontSize: 11, color: "var(--text-muted)" }}>{multi.data.exact.hits}/{multi.data.exact.drawsTested}</div>
                </div>
                <div>
                  <div style={{ fontSize: 12, color: "var(--text-muted)" }}>Last-2-Digit Matches</div>
                  <div style={{ fontSize: 16, fontWeight: 700 }}>{(multi.data.last2.modelHitRate * 100).toFixed(1)}%</div>
                  <div style={{ fontSize: 11, color: "var(--text-muted)" }}>{multi.data.last2.hits}/{multi.data.last2.drawsTested}</div>
                </div>
                <div>
                  <div style={{ fontSize: 12, color: "var(--text-muted)" }}>Last-3-Digit Matches</div>
                  <div style={{ fontSize: 16, fontWeight: 700 }}>{(multi.data.last3.modelHitRate * 100).toFixed(1)}%</div>
                  <div style={{ fontSize: 11, color: "var(--text-muted)" }}>{multi.data.last3.hits}/{multi.data.last3.drawsTested}</div>
                </div>
              </div>
            </div>
          )}

          <p className="disclaimer" style={{ padding: 0 }}>{backtest.data.disclaimer}</p>
        </>
      )}
    </div>
  );
}

const MODEL_ROWS = [
  { key: "multiFactor", label: "Multi-Factor (current)" },
  { key: "frequencyOnly", label: "Frequency-only" },
  { key: "recencyOnly", label: "Recency-only" },
  { key: "random", label: "Random (analytical)" },
] as const;

function ModelComparisonTab() {
  const comparison = useApi(() => analysisApi.modelComparison({ drawCount: 20, candidateCount: 10 }), []);
  if (comparison.loading) return <LoadingSkeleton rows={3} height={160} />;
  if (comparison.error) return <ErrorState message={comparison.error} onRetry={comparison.reload} />;
  if (!comparison.data || comparison.data.byDrawTime.length === 0) return <EmptyState message="Not enough historical data yet." />;

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
      <p style={{ fontSize: 12, color: "var(--text-muted)", margin: 0 }}>
        Read-only comparison: the current Multi-Factor model against simple single-factor baselines, using identical chronological test windows (no future-data leakage). Not a claim of predictability.
      </p>
      {comparison.data.byDrawTime.map((d) => (
        <div key={d.drawTime} className="card" style={{ overflowX: "auto" }}>
          <div className="section-title" style={{ marginTop: 0 }}>{d.drawTime}</div>
          <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 12 }}>
            <thead>
              <tr style={{ textAlign: "left", color: "var(--text-muted)" }}>
                <th style={{ padding: "4px 8px 4px 0" }}>Model</th>
                <th style={{ padding: "4px 8px" }}>Exact</th>
                <th style={{ padding: "4px 8px" }}>Last-3</th>
                <th style={{ padding: "4px 8px" }}>Last-2</th>
              </tr>
            </thead>
            <tbody>
              {MODEL_ROWS.map((row) => {
                const r = d[row.key];
                return (
                  <tr key={row.key} style={{ borderTop: "1px solid var(--border)" }}>
                    <td style={{ padding: "6px 8px 6px 0", fontWeight: 600 }}>{row.label}</td>
                    <td style={{ padding: "6px 8px" }}>{(r.exact.hitRate * 100).toFixed(2)}%</td>
                    <td style={{ padding: "6px 8px" }}>{(r.last3.hitRate * 100).toFixed(2)}%</td>
                    <td style={{ padding: "6px 8px" }}>{(r.last2.hitRate * 100).toFixed(1)}%</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          <p style={{ fontSize: 11, color: "var(--text-muted)", margin: "6px 0 0" }}>
            {d.multiFactor.last2.drawsTested} draws tested
          </p>
        </div>
      ))}
      <p className="disclaimer" style={{ padding: 0 }}>{comparison.data.disclaimer}</p>
    </div>
  );
}
