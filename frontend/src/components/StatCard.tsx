import type { ReactNode } from "react";
import { Link } from "react-router-dom";

export default function StatCard({
  title,
  icon,
  children,
  to,
}: {
  title: string;
  icon?: ReactNode;
  children: ReactNode;
  to?: string;
}) {
  const content = (
    <div className="card" style={{ display: "flex", flexDirection: "column", gap: 8, height: "100%" }}>
      <div style={{ display: "flex", alignItems: "center", gap: 6, color: "var(--text-muted)", fontSize: 12, fontWeight: 600 }}>
        {icon}
        <span>{title}</span>
      </div>
      <div>{children}</div>
    </div>
  );
  return to ? <Link to={to} style={{ textDecoration: "none", color: "inherit" }}>{content}</Link> : content;
}

export function ChipRow({ values }: { values: string[] }) {
  if (values.length === 0) return <span style={{ fontSize: 12, color: "var(--text-muted)" }}>No data yet</span>;
  return (
    <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
      {values.map((v, i) => (
        <span
          key={`${v}-${i}`}
          style={{
            fontSize: 13,
            fontWeight: 700,
            background: "var(--bg)",
            border: "1px solid var(--border)",
            borderRadius: 8,
            padding: "3px 8px",
          }}
        >
          {v}
        </span>
      ))}
    </div>
  );
}
