import { NavLink, Outlet } from "react-router-dom";
import { HomeIcon, ResultsIcon, AnalysisIcon, HistoryIcon, SettingsIcon } from "./icons";

const NAV_ITEMS = [
  { to: "/", label: "Home", icon: HomeIcon, end: true },
  { to: "/results", label: "Results", icon: ResultsIcon, end: false },
  { to: "/analysis", label: "Analysis", icon: AnalysisIcon, end: false },
  { to: "/history", label: "History", icon: HistoryIcon, end: false },
  { to: "/settings", label: "Settings", icon: SettingsIcon, end: false },
];

export default function Layout() {
  return (
    <div style={{ display: "flex", minHeight: "100vh" }}>
      <aside className="sidebar">
        <div className="sidebar-brand">LotteryAnalytics</div>
        <nav style={{ display: "flex", flexDirection: "column", gap: 2 }}>
          {NAV_ITEMS.map(({ to, label, icon: Icon, end }) => (
            <NavLink key={to} to={to} end={end} className={({ isActive }) => `sidebar-link${isActive ? " active" : ""}`}>
              <Icon size={18} />
              <span>{label}</span>
            </NavLink>
          ))}
        </nav>
      </aside>

      <div style={{ flex: 1, minWidth: 0 }}>
        <header className="app-header">
          <span className="app-header-title">LotteryAnalytics</span>
        </header>

        <main>
          <Outlet />
        </main>

        <nav className="bottom-nav">
          {NAV_ITEMS.map(({ to, label, icon: Icon, end }) => (
            <NavLink key={to} to={to} end={end} className={({ isActive }) => `bottom-nav-link${isActive ? " active" : ""}`}>
              <Icon size={22} />
              <span>{label}</span>
            </NavLink>
          ))}
        </nav>
      </div>
    </div>
  );
}
