/**
 * ScanRecord Model
 * Represents a single QR code scan event including the decoded
 * payload, payload type, scan timestamp, and associated threat result.
 */
import { ThreatResult } from './ThreatResult';

export class ScanRecord {
  constructor({
    scanId       = Math.random().toString(36).slice(2),
    payload      = '',
    payloadType  = 'url',
    scannedAt    = new Date().toISOString(),
    threatResult = null,
  } = {}) {
    this.scanId      = scanId;
    this.payload     = payload;
    this.payloadType = payloadType;
    this.scannedAt   = scannedAt;
    this.threatResult = threatResult instanceof ThreatResult
      ? threatResult
      : new ThreatResult(threatResult || {});
  }

  /** Returns a locale-formatted scan date string. */
  getFormattedDate() {
    return new Date(this.scannedAt).toLocaleDateString('en-SG', {
      day:    '2-digit',
      month:  'short',
      year:   'numeric',
      hour:   '2-digit',
      minute: '2-digit',
    });
  }

  /** Returns a truncated version of the payload for display. */
  getTruncatedPayload(maxLen = 52) {
    return this.payload.length > maxLen
      ? this.payload.slice(0, maxLen) + '…'
      : this.payload;
  }

  /** Returns a plain object suitable for JSON serialisation. */
  toJSON() {
    return {
      scanId:       this.scanId,
      payload:      this.payload,
      payloadType:  this.payloadType,
      scannedAt:    this.scannedAt,
      threatResult: this.threatResult,
    };
  }
}
