/**
 * FeedbackService — Singleton
 * Handles anonymous feedback submissions for misclassified scan results.
 * Per FR-13: stores only scanId, classificationIssue, and an optional comment.
 * No personal information, device identifiers, or session data is collected.
 */
export class FeedbackService {
  static #instance = null;
  #feedback = [];

  static getInstance() {
    if (!FeedbackService.#instance) {
      FeedbackService.#instance = new FeedbackService();
    }
    return FeedbackService.#instance;
  }

  /**
   * Submits anonymous feedback for a scan record.
   * TODO: replace with POST /api/feedback
   * @param {Object} params
   * @param {string} params.scanId - The scanId of the related scan record
   * @param {string} params.url - The URL that was scanned (for display/context only)
   * @param {string} params.reportedRiskLevel - The risk level the user believes is correct
   * @param {string} params.comment - Optional free-text reason from the user
   * @returns {Promise<Object>}
   */
  async submitFeedback({ scanId, url, reportedRiskLevel, comment }) {
    await new Promise(r => setTimeout(r, 700));

    if (!reportedRiskLevel) {
      throw new Error('Please select what you believe the correct classification should be.');
    }

    const entry = {
      feedbackId:        'fb-' + Math.random().toString(36).slice(2),
      scanId,
      url,
      reportedRiskLevel,
      comment: (comment || '').trim(),
      submittedAt:       new Date().toISOString(),
    };

    this.#feedback.push(entry);
    return entry;
  }

  /**
   * Returns all submitted feedback entries.
   * TODO: replace with GET /api/feedback (admin only)
   * @returns {Promise<Object[]>}
   */
  async getAll() {
    await new Promise(r => setTimeout(r, 200));
    return [...this.#feedback];
  }
}
