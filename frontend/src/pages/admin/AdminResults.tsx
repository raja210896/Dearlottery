import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { adminApi, adminAuth } from "../../api/admin";
import { useApi, useDebounced } from "../../hooks/useApi";
import { LoadingSkeleton, ErrorState, EmptyState } from "../../components/StateViews";
import Pagination from "../../components/Pagination";
import { DRAW_TIMES } from "../../api/types";
import { ApiError } from "../../api/client";

const emptyForm = { id: 0, drawDate: "", drawTime: "1 PM", resultValue: "" };

export default function AdminResults() {
  const navigate = useNavigate();
  useEffect(() => {
    if (!adminAuth.isLoggedIn()) navigate("/admin/login");
  }, [navigate]);

  const [page, setPage] = useState(1);
  const [drawTime, setDrawTime] = useState("");
  const [search, setSearch] = useState("");
  const debouncedSearch = useDebounced(search);
  const [form, setForm] = useState(emptyForm);
  const [formError, setFormError] = useState<string | null>(null);
  const [matchNote, setMatchNote] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const results = useApi(
    () => adminApi.listResults({ page, pageSize: 15, drawTime: drawTime || undefined, search: debouncedSearch || undefined }),
    [page, drawTime, debouncedSearch]
  );

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setFormError(null);
    setMatchNote(null);
    if (!form.drawDate || !form.resultValue) {
      setFormError("Draw date and result value are required.");
      return;
    }
    setSaving(true);
    try {
      const payload = { drawDate: form.drawDate, drawTime: form.drawTime, resultValue: form.resultValue };
      if (form.id) {
        await adminApi.updateResult(form.id, payload);
      } else {
        const res = await adminApi.createResult(payload);
        if (res.matchedCandidate !== null) {
          setMatchNote(res.matchedCandidate
            ? "This result matched one of the pre-draw Statistical Candidates."
            : "This result did not match the pre-draw Statistical Candidates.");
        }
      }
      setForm(emptyForm);
      results.reload();
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : "Failed to save result.");
    } finally {
      setSaving(false);
    }
  }

  function handleEdit(r: { id: number; drawDate: string; drawTime: string; resultValue: string | null }) {
    setMatchNote(null);
    setForm({ id: r.id, drawDate: r.drawDate, drawTime: r.drawTime, resultValue: r.resultValue || "" });
  }

  async function handleDelete(id: number) {
    await adminApi.deleteResult(id);
    if (form.id === id) setForm(emptyForm);
    results.reload();
  }

  return (
    <div className="container">
      <h1 className="section-title" style={{ marginTop: 4 }}>Manual Results</h1>

      <form onSubmit={handleSubmit} className="card" style={{ display: "flex", gap: 8, flexWrap: "wrap", alignItems: "flex-end", marginBottom: 12 }}>
        <label style={{ fontSize: 12 }}>
          Draw Date<br />
          <input type="date" value={form.drawDate} onChange={(e) => setForm({ ...form, drawDate: e.target.value })} />
        </label>
        <label style={{ fontSize: 12 }}>
          Draw Time<br />
          <select value={form.drawTime} onChange={(e) => setForm({ ...form, drawTime: e.target.value })}>
            {DRAW_TIMES.map((d) => <option key={d} value={d}>{d}</option>)}
          </select>
        </label>
        <label style={{ fontSize: 12 }}>
          Result Value<br />
          <input value={form.resultValue} onChange={(e) => setForm({ ...form, resultValue: e.target.value })} placeholder="e.g. 27" style={{ width: 100 }} />
        </label>
        <button className="btn btn-primary" type="submit" disabled={saving}>{form.id ? "Update" : "Save"}</button>
        {form.id > 0 && (
          <button type="button" className="btn btn-outline" onClick={() => setForm(emptyForm)}>Cancel</button>
        )}
        {formError && <span style={{ color: "var(--danger)", fontSize: 13, width: "100%" }}>{formError}</span>}
        {matchNote && <span style={{ color: "var(--text-muted)", fontSize: 12, width: "100%" }}>{matchNote}</span>}
      </form>

      <div className="card" style={{ display: "flex", gap: 8, flexWrap: "wrap", marginBottom: 12 }}>
        <select value={drawTime} onChange={(e) => { setDrawTime(e.target.value); setPage(1); }}>
          <option value="">All draws</option>
          {DRAW_TIMES.map((d) => <option key={d} value={d}>{d}</option>)}
        </select>
        <input placeholder="Search number..." value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} style={{ flex: 1, minWidth: 140 }} />
      </div>

      {results.loading && <LoadingSkeleton rows={5} height={44} />}
      {results.error && <ErrorState message={results.error} onRetry={results.reload} />}
      {results.data && results.data.items.length === 0 && <EmptyState message="No results yet." />}
      {results.data && results.data.items.length > 0 && (
        <div className="card" style={{ padding: 0 }}>
          {results.data.items.map((r, i) => (
            <div key={r.id} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: "10px 14px", borderTop: i === 0 ? "none" : "1px solid var(--border)" }}>
              <div>
                <div style={{ fontSize: 13, fontWeight: 600 }}>{r.drawDate} · {r.drawTime}</div>
                <div style={{ fontSize: 18, fontWeight: 700 }}>{r.resultValue}</div>
              </div>
              <div style={{ display: "flex", gap: 6 }}>
                <button className="btn btn-outline" style={{ padding: "5px 10px", minHeight: 30, fontSize: 12 }} onClick={() => handleEdit(r)}>Edit</button>
                <button className="btn btn-outline" style={{ padding: "5px 10px", minHeight: 30, fontSize: 12, color: "var(--danger)", borderColor: "var(--danger)" }} onClick={() => handleDelete(r.id)}>Delete</button>
              </div>
            </div>
          ))}
        </div>
      )}

      {results.data && (
        <Pagination page={page} pageSize={15} totalCount={results.data.totalCount} onPageChange={setPage} />
      )}
    </div>
  );
}
