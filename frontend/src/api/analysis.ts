import { api, qs } from "./client";
import type {
  AnalysisOverview, BacktestResponse, CandidateResponse, DataQualitySummary, DigitAnalysis,
  FrequencySnapshot, ModelComparisonResponse, MultiDigitBacktestSummary, PatternStats, RecencyEntry,
} from "./types";

export const analysisApi = {
  overview: (drawTime?: string) => api.get<AnalysisOverview>(`/analysis/overview${qs({ drawTime })}`),
  frequency: (params: { drawTime?: string; from?: string; to?: string }) =>
    api.get<FrequencySnapshot>(`/analysis/frequency${qs(params)}`),
  recency: (params: { drawTime?: string; digitLength?: number; recentWindow?: number }) =>
    api.get<RecencyEntry[]>(`/analysis/recency${qs(params)}`),
  patterns: (params: { drawTime?: string; digitLength?: number; from?: string; to?: string }) =>
    api.get<PatternStats>(`/analysis/patterns${qs(params)}`),
  candidates: (params: { draw?: string; digitLength?: number; from?: string; to?: string; count?: number }) =>
    api.get<CandidateResponse>(`/analysis/candidates${qs(params)}`),
  backtest: (params: { draw?: string; digitLength?: number; drawCount?: number; candidateCount?: number; from?: string; to?: string }) =>
    api.get<BacktestResponse>(`/analysis/backtest${qs(params)}`),
  dataQuality: (draw?: string) => api.get<DataQualitySummary>(`/analysis/backtest/data-quality${qs({ draw })}`),
  backtestMulti: (params: { draw?: string; drawCount?: number; candidateCount?: number; from?: string; to?: string }) =>
    api.get<MultiDigitBacktestSummary>(`/analysis/backtest/multi${qs(params)}`),
  digits: (params: { drawTime?: string; from?: string; to?: string; recentWindow?: number }) =>
    api.get<DigitAnalysis>(`/analysis/digits${qs(params)}`),
  modelComparison: (params: { drawCount?: number; candidateCount?: number; from?: string; to?: string }) =>
    api.get<ModelComparisonResponse>(`/analysis/model-comparison${qs(params)}`),
};
