/**
 * ScanHistoryService — Singleton
 * CRUD for the logged-in user's scan history, backed by the
 * ASP.NET Core /api/ScanHistory endpoints (requires an authenticated
 * session — the backend scopes records to the current user).
 */
import { ScanRecord }   from '../models/ScanRecord';
import { ThreatResult } from '../models/ThreatResult';
import { apiFetch }     from '../api';

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

function toThreatResult(raw) {
  const riskLevel = RISK_LEVEL_BY_SERVICE_RESULT[raw.serviceResultEnum] || ThreatResult.LEVELS.SAFE;
  const votes = raw.serviceScanResult || [];
  const agreeing = votes.filter(v => v.serviceResult === raw.serviceResultEnum);
  const confidenceScore = votes.length ? Math.round((agreeing.length / votes.length) * 100) : 0;
  const onnxVote = votes.find(v => v.vendor === 'ONNX');

  return new ThreatResult({
    riskLevel,
    confidenceScore,
    explanation: (agreeing.length ? agreeing : votes).map(v => `${v.vendor}: ${v.reasons.join(', ')}`).join(' '),
    recommendation: RECOMMENDATION_BY_RISK_LEVEL[riskLevel],
    sources: votes.map(v => v.vendor),
    mlPrediction: onnxVote?.serviceResult ?? null,
    raw,
  });
}

function toScanRecord(dto) {
  return new ScanRecord({
    scanId:       String(dto.id),
    payload:      dto.payload,
    payloadType:  dto.payloadType,
    scannedAt:    dto.scannedAt,
    threatResult: toThreatResult(dto.results),
  });
}

export class ScanHistoryService {
  static #instance = null;

  static getInstance() {
    if (!ScanHistoryService.#instance) {
      ScanHistoryService.#instance = new ScanHistoryService();
    }
    return ScanHistoryService.#instance;
  }

  /**
   * Retrieves all scan records for the current user, most recent first.
   * @returns {Promise<ScanRecord[]>}
   */
  async getAll() {
    const dtos = await apiFetch('/api/ScanHistory');
    return dtos.map(toScanRecord);
  }

  /**
   * Saves a scan record to the current user's history.
   * @param {ScanRecord} record
   * @returns {Promise<ScanRecord>}
   */
  async save(record) {
    const dto = await apiFetch('/api/ScanHistory', {
      method: 'POST',
      body: {
        Payload:     record.payload,
        PayloadType: record.payloadType,
        Result:      record.threatResult.raw,
      },
    });
    return toScanRecord(dto);
  }

  /**
   * Deletes a scan record by ID.
   * @param {string} scanId
   */
  async delete(scanId) {
    await apiFetch(`/api/ScanHistory/${scanId}`, { method: 'DELETE' });
  }

  /**
   * Searches scan records by URL substring or risk level.
   * @param {string} query
   * @returns {Promise<ScanRecord[]>}
   */
  async search(query) {
    const q = query.toLowerCase();
    const all = await this.getAll();
    return all.filter(r =>
      r.payload.toLowerCase().includes(q) ||
      r.threatResult.riskLevel.includes(q)
    );
  }
}
