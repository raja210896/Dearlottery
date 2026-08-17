import { lazy, Suspense } from "react";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import Layout from "./components/Layout";
import { LoadingSkeleton } from "./components/StateViews";

const Home = lazy(() => import("./pages/Home"));
const Results = lazy(() => import("./pages/Results"));
const Analysis = lazy(() => import("./pages/Analysis"));
const PredictionHistory = lazy(() => import("./pages/PredictionHistory"));
const History = lazy(() => import("./pages/History"));
const Settings = lazy(() => import("./pages/Settings"));
const About = lazy(() => import("./pages/About"));
const Admin = lazy(() => import("./pages/admin/Admin"));
const AdminLogin = lazy(() => import("./pages/admin/AdminLogin"));
const AdminResults = lazy(() => import("./pages/admin/AdminResults"));
const AdminImport = lazy(() => import("./pages/admin/AdminImport"));

function PageFallback() {
  return (
    <div className="container">
      <LoadingSkeleton rows={3} height={80} />
    </div>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <Suspense fallback={<PageFallback />}>
        <Routes>
          <Route element={<Layout />}>
            <Route path="/" element={<Home />} />
            <Route path="/results" element={<Results />} />
            <Route path="/analysis" element={<Analysis />} />
            <Route path="/analysis/history" element={<PredictionHistory />} />
            <Route path="/history" element={<History />} />
            <Route path="/settings" element={<Settings />} />
            <Route path="/about" element={<About />} />
          </Route>
          <Route path="/admin/login" element={<AdminLogin />} />
          <Route path="/admin" element={<Admin />} />
          <Route path="/admin/results" element={<AdminResults />} />
          <Route path="/admin/import" element={<AdminImport />} />
        </Routes>
      </Suspense>
    </BrowserRouter>
  );
}
