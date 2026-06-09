'use client';
/**
 * ResultPage (/result)
 * Displays the threat analysis result for the most recently scanned QR payload.
 * Reads the scan record from sessionStorage (set by ScannerPage or UploadPage).
 */
import { useState, useEffect } from 'react';
import { useRouter }           from 'next/navigation';
import { Navbar, RiskBadge, WarningModal, Spinner } from '../../components';
import { ScanHistoryService }  from '../../lib/services/ScanHistoryService';
import { ScanRecord }          from '../../lib/models/ScanRecord';
import { ThreatResult }        from '../../lib/models/ThreatResult';

export default function ResultPage() {
  const router = useRouter();

  const [record,         setRecord]         = useState(null);
  const [showModal,      setShowModal]      = useState(false);
  const [sandboxOpen,    setSandboxOpen]    = useState(false);
  const [saving,         setSaving]         = useState(false);
  const [saved,          setSaved]          = useState(false);

  const historySvc = ScanHistoryService.getInstance();

  // Load result from sessionStorage on mount
  useEffect(() => {
    const raw = sessionStorage.getItem('safeqr_result');
    if (!raw) { router.replace('/scanner'); return; }
    const data = JSON.parse(raw);
    setRecord(new ScanRecord({
      ...data,
      threatResult: new ThreatResult(data.threatResult),
    }));
  }, []);

  const handleSave = async () => {
    setSaving(true);
    await historySvc.save(record);
    setSaving(false);
    setSaved(true);
  };

  if (!record) {
    return (
      <>
        <Navbar activePath="/result" user={null} onLogout={() => {}} />
        <main className="page" style={{ textAlign: 'center', paddingTop: 80 }}>
          <Spinner />
        </main>
      </>
    );
  }

  const { threatResult: result, payload, payloadType } = record;
  const glowColor = {
    safe:       'rgba(0,200,150,0.12)',
    suspicious: 'rgba(245,166,35,0.12)',
    malicious:  'rgba(232,50,90,0.12)',
  }[result.riskLevel];

  const heroIcon = { safe: '✅', suspicious: '⚠️', malicious: '🚫' }[result.riskLevel];

  return (
    <>
      <Navbar activePath="/result" user={null} onLogout={() => {}} />
      <main className="page">
        <div className="page-title">🛡 Safety Result</div>
        <div className="page-sub">Threat analysis complete. Review your result below.</div>

        {/* Sandbox warning modal */}
        {showModal && (
          <WarningModal
            url={payload}
            onConfirm={() => { setShowModal(false); setSandboxOpen(true); }}
            onCancel={() => setShowModal(false)}
          />
        )}

        {/* Hero card */}
        <div
          className="result-hero"
          style={{ backgroundImage: `radial-gradient(ellipse at 50% 0%, ${glowColor}, transparent 70%)` }}
        >
          <div className="result-icon">{heroIcon}</div>
          <RiskBadge level={result.riskLevel} />
          <div className="result-payload">{payload}</div>
          <div className="conf-wrap">
            <div className="conf-label">
              <span>Confidence score</span>
              <span>{result.confidenceScore}%</span>
            </div>
            <div className="conf-bar">
              <div
                className="conf-fill"
                style={{ width: `${result.confidenceScore}%`, background: result.getRiskColor() }}
              />
            </div>
          </div>
        </div>

        {/* Analysis details */}
        <div className="card mt-4">
          <div className="label">Analysis Details</div>
          {[
            ['📋', 'Payload type',     payloadType.toUpperCase(), null],
            ['💬', 'Explanation',      result.explanation,        null],
            ['✔️', 'Recommendation',   result.recommendation,     result.getRiskColor()],
          ].map(([icon, label, val, color]) => (
            <div key={label} className="detail-row">
              <span className="detail-icon">{icon}</span>
              <div>
                <div className="detail-label">{label}</div>
                <div
                  className="detail-val"
                  style={color ? { color, fontWeight: 500 } : {}}
                >
                  {val}
                </div>
              </div>
            </div>
          ))}
          <div className="detail-row">
            <span className="detail-icon">🔎</span>
            <div>
              <div className="detail-label">Sources checked</div>
              <div className="chips">
                {result.sources.map(s => (
                  <span className="chip" key={s}>{s}</span>
                ))}
              </div>
            </div>
          </div>
        </div>

        {/* Sandbox preview — shown for suspicious/malicious URLs only */}
        {(result.isSuspicious() || result.isMalicious()) && payloadType === 'url' && (
          <div className="card mt-4">
            <div
              className="flex-between"
              style={{ marginBottom: sandboxOpen ? 12 : 0 }}
            >
              <div>
                <div className="label" style={{ marginBottom: 2 }}>Sandbox Preview</div>
                <p style={{ fontSize: 13, color: 'var(--text-muted)' }}>
                  Inspect this URL safely in an isolated environment.
                </p>
              </div>
              {!sandboxOpen && (
                <button
                  className="btn btn-warn btn-sm"
                  onClick={() => setShowModal(true)}
                >
                  Open Sandbox
                </button>
              )}
            </div>
            {sandboxOpen && (
              <div className="sandbox-frame">
                <div className="sandbox-msg">
                  🔒 Sandbox active<br />
                  <span style={{ fontSize: 11, opacity: 0.7 }}>
                    Cookies, storage, and device access are blocked.
                  </span>
                  <br /><br />
                  <code style={{ fontSize: 11, color: 'var(--text-muted)', wordBreak: 'break-all' }}>
                    {payload}
                  </code>
                </div>
              </div>
            )}
          </div>
        )}

        {/* Action buttons */}
        <div style={{ display: 'flex', gap: 10, marginTop: 16 }}>
          <button
            className="btn btn-primary"
            style={{ flex: 1 }}
            onClick={() => router.push('/scanner')}
          >
            ← Scan Another
          </button>
          {!saved ? (
            <button className="btn btn-secondary" onClick={handleSave} disabled={saving}>
              {saving ? <Spinner /> : '💾 Save'}
            </button>
          ) : (
            <button className="btn btn-secondary" disabled style={{ color: 'var(--safe)' }}>
              ✅ Saved
            </button>
          )}
          <button className="btn btn-secondary" onClick={() => router.push('/history')}>
            📋 History
          </button>
        </div>
      </main>
    </>
  );
}
