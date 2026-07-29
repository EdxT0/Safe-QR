/**
 * SandboxService — Singleton
 * Requests a static screenshot of a URL from the backend's isolated
 * headless-browser sandbox (POST /api/Sandbox/preview). The page's HTML/JS
 * never reaches this browser — only a rendered PNG does.
 */
import { apiFetch } from '../api';

export class SandboxService {
  static #instance = null;

  static getInstance() {
    if (!SandboxService.#instance) {
      SandboxService.#instance = new SandboxService();
    }
    return SandboxService.#instance;
  }

  /**
   * @param {string} url
   * @returns {Promise<string>} a data: URL suitable for an <img src>
   */
  async capturePreview(url) {
    const { imageBase64 } = await apiFetch('/api/Sandbox/preview', {
      method: 'POST',
      body: { Url: url },
    });
    return `data:image/png;base64,${imageBase64}`;
  }
}
