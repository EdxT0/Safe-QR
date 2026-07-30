/**
 * AdminService — Singleton
 * Pulls the data behind the admin dashboard: system-wide threat analytics
 * (US-07) and the full URL-report / user-feedback lists (US-08). Every
 * endpoint here is [Authorize(Roles = "Admin")] on the backend — a non-admin
 * session gets a 403, an anonymous one gets a 401.
 */
import { apiFetch } from '../api';

export class AdminService {
  static #instance = null;

  static getInstance() {
    if (!AdminService.#instance) {
      AdminService.#instance = new AdminService();
    }
    return AdminService.#instance;
  }

  /**
   * @param {string} [from] - ISO date (yyyy-mm-dd); backend defaults to 30 days ago
   * @param {string} [to] - ISO date (yyyy-mm-dd); backend defaults to today
   */
  async getThreatsAnalytics(from, to) {
    const params = new URLSearchParams();
    if (from) params.set('from', from);
    if (to) params.set('to', to);
    const qs = params.toString();
    return apiFetch(`/api/UrlReport/analytics/threats${qs ? `?${qs}` : ''}`);
  }

  /** All scanned URLs and their verdicts, most recent first. */
  async getAllUrlReports() {
    return apiFetch('/api/UrlReport/All');
  }

  /** All user-submitted misclassification reports, most recent first. */
  async getAllFeedback() {
    return apiFetch('/api/ThreatFeedback');
  }
}
