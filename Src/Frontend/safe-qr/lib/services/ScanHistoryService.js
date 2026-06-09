/**
 * ScanHistoryService — Singleton
 * Manages CRUD operations for a user's scan history.
 * In production, all methods call the ASP.NET Core /api/history endpoints
 * which read/write from the MySQL database.
 */
import { ScanRecord }   from '../models/ScanRecord';
import { ThreatResult } from '../models/ThreatResult';

export class ScanHistoryService {
  static #instance = null;
  #records = [];

  static getInstance() {
    if (!ScanHistoryService.#instance) {
      ScanHistoryService.#instance = new ScanHistoryService();
    }
    return ScanHistoryService.#instance;
  }

  constructor() {
    // Seed with demo records for development
    this.#records = [
      new ScanRecord({
        payload:     'https://www.dbs.com.sg/personal/default.page',
        payloadType: 'url',
        scannedAt:   new Date(Date.now() - 86_400_000).toISOString(),
        threatResult: new ThreatResult({
          riskLevel: 'safe', confidenceScore: 94,
          explanation: 'Verified banking website.',
          recommendation: 'Safe to open.',
          sources: ['VirusTotal', 'Google Safe Browsing'],
        }),
      }),
      new ScanRecord({
        payload:     'http://bit.ly/3xR9mQ2',
        payloadType: 'url',
        scannedAt:   new Date(Date.now() - 172_800_000).toISOString(),
        threatResult: new ThreatResult({
          riskLevel: 'suspicious', confidenceScore: 71,
          explanation: 'Shortened URL via HTTP.',
          recommendation: 'Use sandbox preview before proceeding.',
          sources: ['Rule-Based Engine'],
        }),
      }),
      new ScanRecord({
        payload:     'http://phishing-example-malware.tk/steal',
        payloadType: 'url',
        scannedAt:   new Date(Date.now() - 259_200_000).toISOString(),
        threatResult: new ThreatResult({
          riskLevel: 'malicious', confidenceScore: 97,
          explanation: 'Known phishing and malware URL.',
          recommendation: 'Do not open.',
          sources: ['VirusTotal', 'PhishTank'],
        }),
      }),
    ];
  }

  /**
   * Retrieves all scan records sorted by most recent first.
   * TODO: replace with GET /api/history
   * @returns {Promise<ScanRecord[]>}
   */
  async getAll() {
    await new Promise(r => setTimeout(r, 300));
    return [...this.#records].sort(
      (a, b) => new Date(b.scannedAt) - new Date(a.scannedAt)
    );
  }

  /**
   * Saves a new scan record.
   * TODO: replace with POST /api/history
   * @param {ScanRecord} record
   * @returns {Promise<ScanRecord>}
   */
  async save(record) {
    await new Promise(r => setTimeout(r, 200));
    const exists = this.#records.find(r => r.scanId === record.scanId);
    if (!exists) this.#records.unshift(record);
    return record;
  }

  /**
   * Deletes a scan record by ID.
   * TODO: replace with DELETE /api/history/:scanId
   * @param {string} scanId
   */
  async delete(scanId) {
    await new Promise(r => setTimeout(r, 200));
    this.#records = this.#records.filter(r => r.scanId !== scanId);
  }

  /**
   * Searches scan records by URL substring or risk level.
   * TODO: replace with GET /api/history?q=...
   * @param {string} query
   * @returns {Promise<ScanRecord[]>}
   */
  async search(query) {
    const q = query.toLowerCase();
    return this.#records.filter(r =>
      r.payload.toLowerCase().includes(q) ||
      r.threatResult.riskLevel.includes(q)
    );
  }
}
