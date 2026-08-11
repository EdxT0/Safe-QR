/**
 * Shared fetch helper for talking to the ASP.NET Core backend.
 * Sends cookies (credentials: 'include') so the "LoginCookie" session
 * survives across the cross-origin dev setup (Next.js on :3000, API on :56166).
 */
export const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || 'https://localhost:56166';

export class ApiError extends Error {
  constructor(message, status) {
    super(message);
    this.status = status;
  }
}

export async function apiFetch(path, { method = 'GET', body, signal } = {}) {
  const res = await fetch(`${API_BASE_URL}${path}`, {
    method,
    credentials: 'include',
    headers: {
      // Skips ngrok's free-tier HTML interstitial, which would otherwise be
      // returned in place of the real JSON response when the backend is
      // tunnelled through an *.ngrok-free.app URL.
      'ngrok-skip-browser-warning': 'true',
      ...(body !== undefined ? { 'Content-Type': 'application/json' } : {}),
    },
    body: body !== undefined ? JSON.stringify(body) : undefined,
    signal,
  });

  if (!res.ok) {
    const text = await res.text().catch(() => '');
    throw new ApiError(text || `Request failed with status ${res.status}`, res.status);
  }

  if (res.status === 204) return null;

  const text = await res.text();
  return text ? JSON.parse(text) : null;
}
