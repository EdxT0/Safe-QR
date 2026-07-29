'use client';
/**
 * ResultPage (/result)
 * Displays the threat analysis result for the most recently scanned QR payload.
 * Reads the scan record from sessionStorage (set by ScannerPage or UploadPage).
 */
import { useState, useEffect } from 'react';
import { useRouter }           from 'next/navigation';
import { Navbar, RiskBadge, WarningModal, Spinner } from '../../components';
import { useUser }             from '../../components/UserContext';
import { ScanHistoryService }  from '../../lib/services/ScanHistoryService';
import { SandboxService }      from '../../lib/services/SandboxService';
import { ScanRecord }          from '../../lib/models/ScanRecord';
import { ThreatResult }        from '../../lib/models/ThreatResult';

export default function ResultPage() {
  const router = useRouter();
  const { user } = useUser();

  const [record,         setRecord]         = useState(null);
  const [showModal,      setShowModal]      = useState(false);
  const [sandboxOpen,    setSandboxOpen]    = useState(false);
  const [sandboxLoading, setSandboxLoading] = useState(false);
  const [sandboxImage,   setSandboxImage]   = useState('');
  const [sandboxError,   setSandboxError]   = useState('');
  const [saving,         setSaving]         = useState(false);
  const [saved,          setSaved]          = useState(false);
  const [saveError,      setSaveError]      = useState('');

  const historySvc = ScanHistoryService.getInstance();
  const sandboxSvc = SandboxService.getInstance();

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

  const openSandbox = async () => {
    setSandboxOpen(true);
    setSandboxError('');
    setSandboxLoading(true);
    try {
      const image = await sandboxSvc.capturePreview(payload);
      setSandboxImage(image);
    } catch (e) {
      setSandboxError(e.message || 'Could not render a preview of this page.');
    } finally {
      setSandboxLoading(false);
    }
  };

  const handleSave = async () => {
    if (!user) {
      router.push('/login');
      return;
    }
    setSaveError('');
    setSaving(true);
    try {
      await historySvc.save(record);
      setSaved(true);
    } catch (e) {
      setSaveError(e.message || 'Could not save this result. Please try again.');
    } finally {
      setSaving(false);
    }
  };

  if (!record) {
    return (
      <>
        <Navbar activePath="/result" />
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
      <Navbar activePath="/result" />
      <main className="page">
        <div className="page-title">🛡 Safety Result</div>
        <div className="page-sub">Threat analysis complete. Review your result below.</div>

        {/* Sandbox warning modal */}
        {showModal && (
          <WarningModal
            url={payload}
            onConfirm={() => { setShowModal(false); openSandbox(); }}
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
                {sandboxLoading ? (
                  <div className="sandbox-msg">
                    <Spinner /><br />
                    Rendering an isolated preview…
                  </div>
                ) : sandboxError ? (
                  <div className="sandbox-msg">
                    ⚠️ {sandboxError}
                  </div>
                ) : (
                  <div style={{ width: '100%', padding: 10 }}>
                    <img
                      src={sandboxImage}
                      alt={`Sandboxed preview of ${payload}`}
                      style={{ width: '100%', borderRadius: 'var(--radius)', display: 'block' }}
                    />
                    <p style={{ fontSize: 11, color: 'var(--text-muted)', marginTop: 8, textAlign: 'center' }}>
                      🔒 Static screenshot rendered in an isolated backend browser — this
                      page's own HTML/JS never reached your browser.
                    </p>
                  </div>
                )}
              </div>
            )}
          </div>
        )}

        {saveError && <div className="error-msg" style={{ marginTop: 16 }}>{saveError}</div>}

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
              {saving ? <Spinner /> : user ? '💾 Save' : '🔒 Sign in to Save'}
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
