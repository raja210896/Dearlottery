export default function SimpleBarChart({
  bars, height = 120,
}: { bars: { label: string; value: number; color?: string }[]; height?: number }) {
  const max = Math.max(...bars.map((b) => b.value), 0.0001);
  return (
    <div style={{ display: "flex", alignItems: "flex-end", gap: 6, height, padding: "4px 0" }}>
      {bars.map((b) => (
        <div key={b.label} style={{ flex: 1, display: "flex", flexDirection: "column", alignItems: "center", gap: 4, height: "100%", justifyContent: "flex-end" }}>
          <span style={{ fontSize: 11, fontWeight: 700 }}>{(b.value * 100).toFixed(1)}%</span>
          <div
            style={{
              width: "100%",
              maxWidth: 48,
              height: `${(b.value / max) * (height - 40)}px`,
              background: b.color || "var(--primary)",
              borderRadius: "6px 6px 0 0",
              minHeight: 2,
            }}
          />
          <span style={{ fontSize: 11, color: "var(--text-muted)" }}>{b.label}</span>
        </div>
      ))}
    </div>
  );
}
