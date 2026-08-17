import { api, qs } from "./client";
import type { PagedResult, PredictionHistoryDto, PredictionPerformanceDto } from "./types";

export const predictionsApi = {
  save: (payload: { drawDate: string; drawTime: string; digitLength: number; count: number }) =>
    api.post<PredictionHistoryDto>("/analysis/predictions", payload),
  history: (params: {
    from?: string; to?: string; drawTime?: string; digitLength?: number; matchStatus?: string; page?: number; pageSize?: number;
  }) => api.get<PagedResult<PredictionHistoryDto>>(`/analysis/predictions/history${qs(params)}`),
  performance: (drawTime?: string) =>
    api.get<PredictionPerformanceDto>(`/analysis/predictions/performance${qs({ drawTime })}`),
};
