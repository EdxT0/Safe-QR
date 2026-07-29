/**
 * ThreatResult Model
 * Encapsulates the outcome of a URL safety analysis,
 * combining ML classification, rule-based checks, and threat intelligence.
 */
export class ThreatResult {
  static LEVELS = {
    SAFE:       'safe',
    SUSPICIOUS: 'suspicious',
    MALICIOUS:  'malicious',
  };

  constructor({
    riskLevel       = ThreatResult.LEVELS.SAFE,
    confidenceScore = 0,
    explanation     = '',
    recommendation  = '',
    sources         = [],
    mlPrediction    = null,
    raw             = null,
  } = {}) {
    this.riskLevel       = riskLevel;
    this.confidenceScore = confidenceScore;
    this.explanation     = explanation;
    this.recommendation  = recommendation;
    this.sources         = sources;
    this.mlPrediction    = mlPrediction;
    // Raw AggregatedFinalResult from POST /api/Scan, kept so ScanHistoryService
    // can persist the original backend shape when saving this record.
    this.raw              = raw;
  }

  isSafe()       { return this.riskLevel === ThreatResult.LEVELS.SAFE; }
  isSuspicious() { return this.riskLevel === ThreatResult.LEVELS.SUSPICIOUS; }
  isMalicious()  { return this.riskLevel === ThreatResult.LEVELS.MALICIOUS; }

  /** Returns the hex colour associated with this risk level. */
  getRiskColor() {
    const map = {
      safe:       '#00C896',
      suspicious: '#F5A623',
      malicious:  '#E8325A',
    };
    return map[this.riskLevel] || '#888';
  }

  /** Returns a display-friendly risk label. */
  getRiskLabel() {
    const map = {
      safe:       'Safe',
      suspicious: 'Suspicious',
      malicious:  'Malicious',
    };
    return map[this.riskLevel] || 'Unknown';
  }

  /** Returns the emoji icon for this risk level. */
  getRiskIcon() {
    return { safe: '✅', suspicious: '⚠️', malicious: '🚫' }[this.riskLevel] || '🔍';
  }
}
