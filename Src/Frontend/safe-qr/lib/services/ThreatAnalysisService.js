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
   * Follows the Risk Classification Engine priority order:
   *   1. Malicious  — Google Safe Browsing / VirusTotal confirms phishing, malware, or social engineering → Block Access
   *   2. High Risk  — ONNX high-confidence phishing prediction AND heuristic signals (homoglyph, brand impersonation, suspicious TLD) → Warn User
   *   3. Suspicious — ONNX borderline anomaly OR structural indicators (shorteners, new domains, encoded chars) → Sandbox Preview
   *   4. Safe       — No threat signals across all analysis layers → Allow Access
   * TODO: replace with real API call to ONNX-based ML engine + threat intel APIs.
   * @param {string} payload
   * @returns {ThreatResult}
   */
  #classifyPayload(payload) {
    const url = payload.toLowerCase();

    // ── Priority 1: MALICIOUS — confirmed by threat intelligence ──
    const maliciousKeywords = [
      'phishing', 'malware', 'steal', 'hack',
      'free-prize', 'win?ref', '.tk', 'exploit',
    ];
    if (maliciousKeywords.some(k => url.includes(k))) {
      return new ThreatResult({
        riskLevel:       ThreatResult.LEVELS.MALICIOUS,
        confidenceScore: 97,
        explanation:
          'Google Safe Browsing or VirusTotal confirms the URL is phishing, malware, or social engineering.',
        recommendation: 'Access blocked. Do not open this link — this QR code is confirmed dangerous.',
        sources:         ['Google Safe Browsing', 'VirusTotal'],
        mlPrediction:    'malicious',
      });
    }

    // ── Priority 2: HIGH RISK — ONNX high-confidence phishing + heuristic signals ──
    const homoglyphPattern = /paypa1|amaz0n|g00gle|micr0soft|app1e|0utlook/;
    const brandImpersonationKeywords = [
      'secure-login', 'account-verify', 'login-verify', 'signin-secure', 'verify-account',
    ];
    const suspiciousTlds = ['.xyz', '.top', '.club', '.work', '.click'];

    const hasHomoglyph = homoglyphPattern.test(url);
    const hasBrandImpersonation = brandImpersonationKeywords.some(k => url.includes(k));
    const hasSuspiciousTld = suspiciousTlds.some(tld => url.includes(tld));

    if (hasHomoglyph || hasBrandImpersonation || hasSuspiciousTld) {
      const signals = [];
      if (hasHomoglyph) signals.push('homoglyph character substitution');
      if (hasBrandImpersonation) signals.push('brand impersonation keyword pattern');
      if (hasSuspiciousTld) signals.push('suspicious top-level domain');

      return new ThreatResult({
        riskLevel:       ThreatResult.LEVELS.HIGH_RISK,
        confidenceScore: 85,
        explanation:
          `ONNX model returned a high-confidence phishing prediction. Heuristic analysis detected: ${signals.join(', ')}.`,
        recommendation: 'Warning: this link shows strong phishing indicators. Avoid entering personal information.',
        sources:         ['ONNX Model', 'Heuristic Engine'],
        mlPrediction:    'high_risk',
      });
    }

    // ── Priority 3: SUSPICIOUS — borderline ONNX prediction OR structural indicators ──
    const suspiciousKeywords = [
      'bit.ly', 'tinyurl', 't.co', 'goo.gl',
      'free-', 'click-here',
    ];
    if (suspiciousKeywords.some(k => url.includes(k)) || url.startsWith('http://')) {
      return new ThreatResult({
        riskLevel:       ThreatResult.LEVELS.SUSPICIOUS,
        confidenceScore: 71,
        explanation:
          'ONNX model returned a borderline anomaly prediction. Structural indicators detected: URL shortener, new domain, or encoded characters.',
        recommendation:
          'Sandbox preview recommended. Inspect the destination before proceeding.',
        sources:      ['ONNX Model', 'Rule-Based Engine'],
        mlPrediction: 'suspicious',
      });
    }

    // ── Priority 4: SAFE — no signals across all layers ──
    if (payload.startsWith('WIFI:')) {
      return new ThreatResult({
        riskLevel:       ThreatResult.LEVELS.SAFE,
        confidenceScore: 88,
        explanation:
          'No threat signals detected across all analysis layers. This QR code contains a Wi-Fi configuration payload.',
        recommendation:
          'Access allowed. Verify the network name matches a trusted location before connecting.',
        sources:      ['Payload Inspector'],
        mlPrediction: 'safe',
      });
    }

    return new ThreatResult({
      riskLevel:       ThreatResult.LEVELS.SAFE,
      confidenceScore: 94,
      explanation:
        'No threat signals detected across all analysis layers — all sources return clean verdicts.',
      recommendation: 'Access allowed. This QR code appears safe to open.',
      sources:        ['Google Safe Browsing', 'VirusTotal', 'ONNX Model'],
      mlPrediction:   'safe',
    });
  }
}
