import { useState } from "react";
import { DRAW_TIMES } from "../api/types";
import { subscribeToPush, unsubscribeFromPush } from "../api/notifications";

const PREFS_KEY = "la_notification_prefs";

interface Prefs {
  enabled: boolean;
  drawTimePreference: string; // "" = all draws
  resultNotify: boolean;
  analysisNotify: boolean;
  dailyReminder: boolean;
}

const defaultPrefs: Prefs = { enabled: false, drawTimePreference: "", resultNotify: true, analysisNotify: false, dailyReminder: false };

function loadPrefs(): Prefs {
  try {
    const raw = localStorage.getItem(PREFS_KEY);
    return raw ? { ...defaultPrefs, ...JSON.parse(raw) } : defaultPrefs;
  } catch {
    return defaultPrefs;
  }
}

export default function Settings() {
  const [prefs, setPrefs] = useState<Prefs>(loadPrefs);
  const [status, setStatus] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  function save(next: Prefs) {
    setPrefs(next);
    localStorage.setItem(PREFS_KEY, JSON.stringify(next));
  }

  async function handleToggleEnabled(enabled: boolean) {
    setStatus(null);
    setBusy(true);
    try {
      if (enabled) {
        const perm = await Notification.requestPermission();
        if (perm !== "granted") {
          setStatus("Notification permission was not granted.");
          setBusy(false);
          return;
        }
        await subscribeToPush({
          drawTimePreference: prefs.drawTimePreference || null,
          resultNotify: prefs.resultNotify,
          analysisNotify: prefs.analysisNotify,
          dailyReminder: prefs.dailyReminder,
        });
        save({ ...prefs, enabled: true });
        setStatus("Notifications enabled.");
      } else {
        await unsubscribeFromPush();
        save({ ...prefs, enabled: false });
        setStatus("Notifications disabled.");
      }
    } catch (err) {
      setStatus(err instanceof Error ? err.message : "Something went wrong.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="container">
      <h1 className="section-title" style={{ marginTop: 4 }}>Settings</h1>

      <div className="card" style={{ display: "flex", flexDirection: "column", gap: 14 }}>
        <label style={{ display: "flex", justifyContent: "space-between", alignItems: "center", fontSize: 14, fontWeight: 600 }}>
          Enable Notifications
          <input type="checkbox" checked={prefs.enabled} disabled={busy} onChange={(e) => handleToggleEnabled(e.target.checked)} />
        </label>

        <label style={{ display: "flex", justifyContent: "space-between", alignItems: "center", fontSize: 13 }}>
          Selected Draw
          <select
            value={prefs.drawTimePreference}
            onChange={(e) => save({ ...prefs, drawTimePreference: e.target.value })}
          >
            <option value="">All draws</option>
            {DRAW_TIMES.map((d) => <option key={d} value={d}>{d}</option>)}
          </select>
        </label>

        <label style={{ display: "flex", justifyContent: "space-between", alignItems: "center", fontSize: 13 }}>
          Result Notifications
          <input type="checkbox" checked={prefs.resultNotify} onChange={(e) => save({ ...prefs, resultNotify: e.target.checked })} />
        </label>
        <label style={{ display: "flex", justifyContent: "space-between", alignItems: "center", fontSize: 13 }}>
          Analysis Notifications
          <input type="checkbox" checked={prefs.analysisNotify} onChange={(e) => save({ ...prefs, analysisNotify: e.target.checked })} />
        </label>
        <label style={{ display: "flex", justifyContent: "space-between", alignItems: "center", fontSize: 13 }}>
          Daily Reminder
          <input type="checkbox" checked={prefs.dailyReminder} onChange={(e) => save({ ...prefs, dailyReminder: e.target.checked })} />
        </label>

        {status && <p style={{ fontSize: 12, color: "var(--text-muted)" }}>{status}</p>}
      </div>
    </div>
  );
}
