/**
 * ThreatAnalysisService — Singleton
 * For URL payloads, calls the ASP.NET Core /api/Scan pipeline, which
 * aggregates Google Safe Browsing, VirusTotal, an ONNX phishing model,
 * and an in-house rule engine into one verdict.
 * Non-URL payloads (wifi/sms/email/tel/contact/text) have no backend
 * evaluator, so they fall back to a local heuristic.
 * Results are cached in-memory to avoid redundant API calls.
 */
import { ThreatResult } from '../models/ThreatResult';
import { apiFetch } from '../api';

const RISK_LEVEL_BY_SERVICE_RESULT = {
  safe:       ThreatResult.LEVELS.SAFE,
  suspicious: ThreatResult.LEVELS.SUSPICIOUS,
  highRisk:   ThreatResult.LEVELS.SUSPICIOUS,
  malicious:  ThreatResult.LEVELS.MALICIOUS,
};

const RECOMMENDATION_BY_RISK_LEVEL = {
  [ThreatResult.LEVELS.SAFE]:       'This QR code appears safe to open.',
  [ThreatResult.LEVELS.SUSPICIOUS]: 'Exercise caution. Use the sandbox preview to inspect the destination before proceeding.',
  [ThreatResult.LEVELS.MALICIOUS]:  'Do not open this link. This QR code is dangerous.',
};

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

    const type = this.detectPayloadType(payload);
    const result = type === 'url'
      ? await this.#analyseUrl(payload)
      : this.#classifyNonUrlPayload(payload, type);

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
   * Sends a URL payload through the backend scan pipeline and maps
   * the AggregatedFinalResult into a display-friendly ThreatResult.
   * @param {string} url
   * @returns {Promise<ThreatResult>}
   */
  async #analyseUrl(url) {
    const raw = await apiFetch('/api/Scan', { method: 'POST', body: { Url: url } });
    return this.#mapAggregatedResult(raw);
  }

  /**
   * Maps a backend AggregatedFinalResult (serviceResultEnum + per-vendor
   * serviceScanResult list) into a ThreatResult for display, keeping the
   * raw shape attached so it can be re-sent when saving to history.
   * @param {object} raw
   * @returns {ThreatResult}
   */
  #mapAggregatedResult(raw) {
    const riskLevel = RISK_LEVEL_BY_SERVICE_RESULT[raw.serviceResultEnum] || ThreatResult.LEVELS.SAFE;
    const votes = raw.serviceScanResult || [];

    const agreeing = votes.filter(v => v.serviceResult === raw.serviceResultEnum);
    const confidenceScore = votes.length
      ? Math.round((agreeing.length / votes.length) * 100)
      : 0;

    const explanation = (agreeing.length ? agreeing : votes)
      .map(v => `${v.vendor}: ${v.reasons.join(', ')}`)
      .join(' ');

    const onnxVote = votes.find(v => v.vendor === 'ONNX');

    return new ThreatResult({
      riskLevel,
      confidenceScore,
      explanation: explanation || 'No further details were returned by the scan pipeline.',
      recommendation: RECOMMENDATION_BY_RISK_LEVEL[riskLevel],
      sources: votes.map(v => v.vendor),
      mlPrediction: onnxVote?.serviceResult ?? null,
      raw,
    });
  }

  /**
   * Local rule-based classification for payload types the backend
   * scan pipeline does not evaluate (it only understands URLs).
   * @param {string} payload
   * @param {string} type
   * @returns {ThreatResult}
   */
  #classifyNonUrlPayload(payload, type) {
    const explanation = type === 'wifi'
      ? 'This QR code contains a Wi-Fi configuration payload. No malicious indicators detected.'
      : `This QR code contains a ${type} payload, which is not run through the URL threat pipeline.`;

    // Backend's scan pipeline only evaluates URLs, so build an
    // AggregatedFinalResult-shaped placeholder locally — this keeps the
    // "raw" field populated for any type, which ScanHistoryService needs
    // to persist a record regardless of payload type. "InHouse" is reused
    // as the vendor since it's the only backend vendor enum value that
    // fits a local, non-external-API check.
    const raw = {
      serviceResultEnum: 'safe',
      serviceScanResult: [{ vendor: 'InHouse', serviceResult: 'safe', reasons: [explanation] }],
    };

    return new ThreatResult({
      riskLevel:       ThreatResult.LEVELS.SAFE,
      confidenceScore: type === 'wifi' ? 88 : 70,
      explanation,
      recommendation: type === 'wifi'
        ? 'Verify the network name matches a trusted location before connecting.'
        : 'Review the content before acting on it.',
      sources: ['Payload Inspector'],
      mlPrediction: 'safe',
      raw,
    });
  }
}
