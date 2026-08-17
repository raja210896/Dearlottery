import { api, qs } from "./client";
import type { PagedResult, ResultDto } from "./types";

export const resultsApi = {
  today: () => api.get<ResultDto[]>("/results/today"),
  list: (params: { page?: number; pageSize?: number; drawTime?: string; search?: string }) =>
    api.get<PagedResult<ResultDto>>(`/results${qs(params)}`),
  history: (params: {
    from?: string; to?: string; drawTime?: string; page?: number; pageSize?: number; sort?: string;
  }) => api.get<PagedResult<ResultDto>>(`/results/history${qs(params)}`),
};
