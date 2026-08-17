export default function Pagination({
  page, pageSize, totalCount, onPageChange,
}: { page: number; pageSize: number; totalCount: number; onPageChange: (p: number) => void }) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  if (totalPages <= 1) return null;
  return (
    <div style={{ display: "flex", justifyContent: "center", alignItems: "center", gap: 10, padding: "14px 0" }}>
      <button className="btn btn-outline" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>Prev</button>
      <span style={{ fontSize: 13, color: "var(--text-muted)" }}>{page} / {totalPages}</span>
      <button className="btn btn-outline" disabled={page >= totalPages} onClick={() => onPageChange(page + 1)}>Next</button>
    </div>
  );
}
