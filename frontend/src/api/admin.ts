import { ApiError, qs } from "./client";
import type { ApiResponse, PagedResult, ResultDto } from "./types";

const BASE_URL = import.meta.env.VITE_API_BASE_URL || "http://localhost:5011/api";
const TOKEN_KEY = "la_admin_token";

export const adminAuth = {
  getToken: () => localStorage.getItem(TOKEN_KEY),
  setToken: (token: string) => localStorage.setItem(TOKEN_KEY, token),
  clearToken: () => localStorage.removeItem(TOKEN_KEY),
  isLoggedIn: () => !!localStorage.getItem(TOKEN_KEY),
};

async function authedRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const token = adminAuth.getToken();
  let res: Response;
  try {
    res = await fetch(`${BASE_URL}${path}`, {
      headers: {
        "Content-Type": "application/json",
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...(init?.headers || {}),
      },
      ...init,
    });
  } catch {
    throw new ApiError("Unable to reach the server.");
  }

  if (res.status === 401) {
    adminAuth.clearToken();
    throw new ApiError("Session expired. Please log in again.");
  }

  let body: ApiResponse<T> | null = null;
  try {
    body = await res.json();
  } catch {
    // no body
  }

  if (!res.ok || !body?.success) {
    throw new ApiError(body?.message || `Request failed (${res.status}).`);
  }
  return body.data as T;
}

export interface LoginResponse { token: string; username: string; expiresAt: string }
export interface DashboardSummary {
  totalResults: number;
  latestSyncAt: string | null;
  latestSyncSuccess: boolean;
  latestSyncMessage: string | null;
  syncLogCount: number;
}
export interface SyncLogDto {
  id: number; startedAt: string; completedAt: string | null;
  success: boolean; recordsImported: number; message: string | null; trigger: string;
}

export const adminApi = {
  login: async (username: string, password: string) => {
    const res = await fetch(`${BASE_URL}/admin/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username, password }),
    });
    const body: ApiResponse<LoginResponse> = await res.json();
    if (!res.ok || !body.success) throw new ApiError(body.message || "Login failed.");
    return body.data as LoginResponse;
  },
  dashboard: () => authedRequest<DashboardSummary>("/admin/dashboard"),
  syncLogs: (page = 1, pageSize = 20) => authedRequest<PagedResult<SyncLogDto>>(`/admin/sync-logs${qs({ page, pageSize })}`),
  runSync: () => authedRequest<{ success: boolean; imported: number; message: string | null }>("/admin/sync", { method: "POST" }),

  listResults: (params: { page?: number; pageSize?: number; drawTime?: string; search?: string; from?: string; to?: string }) =>
    authedRequest<PagedResult<ResultDto>>(`/admin/results${qs(params)}`),
  createResult: (payload: { drawDate: string; drawTime: string; resultValue: string }) =>
    authedRequest<{ result: ResultDto; matchedCandidate: boolean | null }>("/admin/results", { method: "POST", body: JSON.stringify(payload) }),
  updateResult: (id: number, payload: { drawDate: string; drawTime: string; resultValue: string }) =>
    authedRequest<{ result: ResultDto }>(`/admin/results/${id}`, { method: "PUT", body: JSON.stringify(payload) }),
  deleteResult: (id: number) => authedRequest<void>(`/admin/results/${id}`, { method: "DELETE" }),
};
