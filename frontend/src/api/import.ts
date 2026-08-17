import { ApiError } from "./client";
import { adminAuth } from "./admin";
import type { ApiResponse, ImportSummary } from "./types";

const BASE_URL = import.meta.env.VITE_API_BASE_URL || "http://localhost:5011/api";

async function uploadFile(path: string, file: File): Promise<ImportSummary> {
  const token = adminAuth.getToken();
  const form = new FormData();
  form.append("file", file);

  let res: Response;
  try {
    res = await fetch(`${BASE_URL}${path}`, {
      method: "POST",
      headers: token ? { Authorization: `Bearer ${token}` } : undefined,
      body: form,
    });
  } catch {
    throw new ApiError("Unable to reach the server.");
  }

  if (res.status === 401) {
    adminAuth.clearToken();
    throw new ApiError("Session expired. Please log in again.");
  }

  let body: ApiResponse<ImportSummary> | null = null;
  try {
    body = await res.json();
  } catch {
    // no body
  }

  if (!res.ok || !body?.success) {
    throw new ApiError(body?.message || `Import failed (${res.status}).`);
  }
  return body.data as ImportSummary;
}

export const importApi = {
  csv: (file: File) => uploadFile("/admin/import/csv", file),
  json: (file: File) => uploadFile("/admin/import/json", file),
};
