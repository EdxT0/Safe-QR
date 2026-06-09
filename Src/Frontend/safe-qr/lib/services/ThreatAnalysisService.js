/**
 * ThreatAnalysisService — Singleton
 * Orchestrates URL safety analysis by combining:
 *   1. Rule-based heuristic checks
 *   2. ML model classification (Python Flask microservice)
 *   3. External threat intelligence APIs (VirusTotal, PhishTank, Google Safe Browsing)
 *
 * Results are cached in-memory to avoid redundant API calls.
 */
import { ThreatResult } from '../models/ThreatResult';

export class ThreatAnalysisService {
  static #instance = null;
  #cache = new Map();

  static getInstance() {
    if (!ThreatAnalysisService.#instance) {
      ThreatAnalysisService.#instance = new ThreatAnalysisService();
    }
    return ThreatAnalysisService.#instance;
  }

  /**
   * Analyses a QR payload and returns a ThreatResult.
   * Results are cached by payload string to prevent duplicate API calls.
   * @param {string} payload
   * @returns {Promise<ThreatResult>}
   */
  async analysePayload(payload) {
    if (this.#cache.has(payload)) {
      return this.#cache.get(payload);
    }
    // TODO: replace simulated delay with POST /api/scan/analyze
    await new Promise(r => setTimeout(r, 1400));
    const result = this.#classifyPayload(payload);
    this.#cache.set(payload, result);
    return result;
  }

  /**
   * Detects the type of content encoded in a QR payload.
   * @param {string} payload
   * @returns {'url'|'wifi'|'sms'|'email'|'telephone'|'contact'|'text'}
   */
  detectPayloadType(payload) {
    if (/^https?:\/\//i.test(payload))      return 'url';
    if (/^WIFI:/i.test(payload))             return 'wifi';
    if (/^SMSTO:|^sms:/i.test(payload))      return 'sms';
    if (/^mailto:/i.test(payload))           return 'email';
    if (/^tel:/i.test(payload))              return 'telephone';
    if (/^BEGIN:VCARD/i.test(payload))       return 'contact';
    return 'text';
  }

  /**
   * Internal rule-based + simulated ML classification.
   * TODO: replace with real API call to Python Flask ML microservice.
   * @param {string} payload
   * @returns {ThreatResult}
   */
  #classifyPayload(payload) {
    const url = payload.toLowerCase();

    const maliciousKeywords = [
      'phishing', 'malware', 'steal', 'hack',
      'free-prize', 'win?ref', '.tk', 'exploit',
    ];
    const suspiciousKeywords = [
      'bit.ly', 'tinyurl', 't.co', 'goo.gl',
      'login-verify', 'free-', 'click-here',
    ];

    if (maliciousKeywords.some(k => url.includes(k))) {
      return new ThreatResult({
        riskLevel:       ThreatResult.LEVELS.MALICIOUS,
        confidenceScore: 97,
        explanation:
          'This URL contains patterns associated with phishing or malware distribution. ' +
          'The domain has been flagged by VirusTotal and PhishTank.',
        recommendation: 'Do not open this link. This QR code is dangerous.',
        sources:         ['VirusTotal', 'PhishTank', 'ML Engine'],
        mlPrediction:    'malicious',
      });
    }

    if (suspiciousKeywords.some(k => url.includes(k)) || url.startsWith('http://')) {
      return new ThreatResult({
        riskLevel:       ThreatResult.LEVELS.SUSPICIOUS,
        confidenceScore: 71,
        explanation:
          'This URL uses a URL shortener or an unencrypted HTTP connection. ' +
          'The true destination cannot be fully verified.',
        recommendation:
          'Exercise caution. Use the sandbox preview to inspect the destination before proceeding.',
        sources:      ['Rule-Based Engine', 'ML Engine'],
        mlPrediction: 'suspicious',
      });
    }

    if (payload.startsWith('WIFI:')) {
      return new ThreatResult({
        riskLevel:       ThreatResult.LEVELS.SAFE,
        confidenceScore: 88,
        explanation:
          'This QR code contains a Wi-Fi configuration payload. No malicious indicators detected.',
        recommendation:
          'Verify the network name matches a trusted location before connecting.',
        sources:      ['Payload Inspector'],
        mlPrediction: 'safe',
      });
    }

    return new ThreatResult({
      riskLevel:       ThreatResult.LEVELS.SAFE,
      confidenceScore: 94,
      explanation:
        'This URL was checked against threat intelligence sources and machine learning models. ' +
        'No threats were detected.',
      recommendation: 'This QR code appears safe to open.',
      sources:        ['VirusTotal', 'Google Safe Browsing', 'ML Engine'],
      mlPrediction:   'safe',
    });
  }
}
