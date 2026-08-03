/**
 * FeedbackService — Singleton
 * Lets a signed-in user report that a scan's threat classification was
 * wrong (POST /api/ThreatFeedback). The backend stores the system's
 * original verdict alongside what the user believes it should have been,
 * for later review and model/rule improvement.
 */
import { apiFetch } from '../api';

export class FeedbackService {
  static #instance = null;

  static getInstance() {
    if (!FeedbackService.#instance) {
      FeedbackService.#instance = new FeedbackService();
    }
    return FeedbackService.#instance;
  }

  /**
   * @param {object} params
   * @param {string} params.payload - the scanned URL/QR content
   * @param {string} params.payloadType
   * @param {object} params.systemClassification - the raw AggregatedFinalResult the backend returned
   * @param {'safe'|'suspicious'|'highRisk'|'malicious'} params.reportedRiskLevel - what the user believes it should be
   * @param {string} [params.comment]
   */
  async submitFeedback({ payload, payloadType, systemClassification, reportedRiskLevel, comment }) {
    await apiFetch('/api/ThreatFeedback', {
      method: 'POST',
      body: {
        Payload: payload,
        PayloadType: payloadType,
        SystemClassification: systemClassification,
        ReportedRiskLevel: reportedRiskLevel,
        Comment: comment || null,
      },
    });
  }
}
