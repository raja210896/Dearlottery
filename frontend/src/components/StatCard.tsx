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
    <div className="card" style={{ display: "flex", flexDirection: "column", gap: 10, height: "100%", transition: "box-shadow 0.15s ease, transform 0.1s ease" }}>
      <div style={{ display: "flex", alignItems: "center", gap: 8, color: "var(--text-muted)", fontSize: 12, fontWeight: 700 }}>
        {icon && (
          <span style={{
            display: "inline-flex", alignItems: "center", justifyContent: "center",
            width: 26, height: 26, borderRadius: 8, background: "var(--primary-tint)", color: "var(--primary)", flexShrink: 0,
          }}>
            {icon}
          </span>
        )}
        <span>{title}</span>
      </div>
      <div>{children}</div>
    </div>
  );
  return to ? (
    <Link
      to={to}
      className="stat-card-link"
      style={{ textDecoration: "none", color: "inherit", display: "block", height: "100%" }}
    >
      {content}
    </Link>
  ) : content;
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
