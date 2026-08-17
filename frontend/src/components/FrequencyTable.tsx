import type { FrequencyEntry } from "../api/types";

export default function FrequencyTable({ entries, limit = 20 }: { entries: FrequencyEntry[]; limit?: number }) {
  const top = entries.slice(0, limit);
  const max = top[0]?.count || 1;

  if (top.length === 0) return <span style={{ fontSize: 13, color: "var(--text-muted)" }}>No data yet.</span>;

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
      {top.map((e) => (
        <div key={e.value} style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <span style={{ width: 36, fontWeight: 700, fontSize: 13 }}>{e.value}</span>
          <div style={{ flex: 1, background: "var(--bg)", borderRadius: 6, height: 8, overflow: "hidden" }}>
            <div style={{ width: `${(e.count / max) * 100}%`, background: "var(--primary)", height: "100%" }} />
          </div>
          <span style={{ width: 24, textAlign: "right", fontSize: 12, color: "var(--text-muted)" }}>{e.count}</span>
        </div>
      ))}
    </div>
  );
}
