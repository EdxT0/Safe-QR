'use client';
/**
 * ResultPage (/result)
 * Displays the threat analysis result for the most recently scanned QR payload.
 *
 * Open link behaviour:
 *   Safe       → "Open Link" button opens directly in a new tab
 *   Suspicious → "Proceed at Own Risk" button shows a warning modal before opening
 *   Malicious  → No open button — link is blocked entirely
 */
import { useState, useEffect } from 'react';
import { useRouter }           from 'next/navigation';
import {
  Navbar, RiskBadge, WarningModal, ProceedModal, FeedbackSection, Spinner,
} from '../../components';
import { ScanHistoryService }  from '../../lib/services/ScanHistoryService';
import { FeedbackService }     from '../../lib/services/FeedbackService';
import { ScanRecord }          from '../../lib/models/ScanRecord';
import { ThreatResult }        from '../../lib/models/ThreatResult';

export default function ResultPage() {
  const router = useRouter();

  const [record,          setRecord]          = useState(null);
  const [showSandbox,     setShowSandbox]     = useState(false);
  const [sandboxOpen,     setSandboxOpen]     = useState(false);
  const [showProceed,     setShowProceed]     = useState(false);
  const [saving,          setSaving]          = useState(false);
  const [saved,           setSaved]           = useState(false);

  const historySvc  = ScanHistoryService.getInstance();
  const feedbackSvc = FeedbackService.getInstance();

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

  /** Opens the URL safely in a new tab with security attributes. */
  const openLink = (url) => {
    const a = document.createElement('a');
    a.href             = url;
    a.target           = '_blank';
    a.rel              = 'noopener noreferrer';  // prevents tab from accessing opener
    a.referrerPolicy   = 'no-referrer';          // no referrer header sent to destination
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
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
  const isUrl = payloadType === 'url';

  const glowColor = {
    safe:       'rgba(0,200,150,0.12)',
    suspicious: 'rgba(245,166,35,0.12)',
    high_risk:  'rgba(232,89,12,0.12)',
    malicious:  'rgba(232,50,90,0.12)',
  }[result.riskLevel];

  const heroIcon = { safe: '✅', suspicious: '⚠️', high_risk: '🔶', malicious: '🚫' }[result.riskLevel];

  return (
    <>
      <Navbar activePath="/result" />
      <main className="page">
        <div className="page-title">🛡 Safety Result</div>
        <div className="page-sub">Threat analysis complete. Review your result below.</div>

        {/* Sandbox warning modal */}
        {showSandbox && (
          <WarningModal
            url={payload}
            onConfirm={() => { setShowSandbox(false); setSandboxOpen(true); }}
            onCancel={() => setShowSandbox(false)}
          />
        )}

        {/* Proceed at own risk modal — suspicious/high risk URLs only */}
        {showProceed && (
          <ProceedModal
            url={payload}
            riskLevel={result.riskLevel}
            onConfirm={() => { setShowProceed(false); openLink(payload); }}
            onCancel={() => setShowProceed(false)}
          />
        )}

        {/* Hero result card */}
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

        {/* ── Open Link section ── */}
        {isUrl && (
          <div className="card mt-4">

            {/* SAFE — direct open button */}
            {result.isSafe() && (
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 16 }}>
                <div>
                  <div className="label" style={{ marginBottom: 2, color: 'var(--safe)' }}>
                    ✅ Safe to Open
                  </div>
                  <p style={{ fontSize: 13, color: 'var(--text-muted)' }}>
                    This link passed all safety checks and is safe to visit.
                  </p>
                </div>
                <button
                  className="btn btn-sm"
                  style={{
                    background: 'var(--safe)', color: '#000',
                    flexShrink: 0, gap: 6,
                    boxShadow: '0 0 16px rgba(0,200,150,0.3)',
                  }}
                  onClick={() => openLink(payload)}
                >
                  🔗 Open Link
                </button>
              </div>
            )}

            {/* SUSPICIOUS — proceed at own risk */}
            {result.isSuspicious() && (
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 16 }}>
                <div>
                  <div className="label" style={{ marginBottom: 2, color: 'var(--warn)' }}>
                    ⚠️ Proceed at Your Own Risk
                  </div>
                  <p style={{ fontSize: 13, color: 'var(--text-muted)' }}>
                    This link is suspicious. We strongly advise using the sandbox preview first.
                  </p>
                </div>
                <button
                  className="btn btn-sm"
                  style={{
                    background: 'rgba(245,166,35,0.12)', color: 'var(--warn)',
                    border: '1px solid rgba(245,166,35,0.4)',
                    flexShrink: 0,
                  }}
                  onClick={() => setShowProceed(true)}
                >
                  ⚠️ Open Anyway
                </button>
              </div>
            )}

            {/* HIGH RISK — strong warning, proceed at own risk */}
            {result.isHighRisk() && (
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 16 }}>
                <div>
                  <div className="label" style={{ marginBottom: 2, color: 'var(--high-risk)' }}>
                    🔶 High Risk — Strong Warning
                  </div>
                  <p style={{ fontSize: 13, color: 'var(--text-muted)' }}>
                    This link shows strong phishing indicators. Opening it is strongly discouraged.
                  </p>
                </div>
                <button
                  className="btn btn-high-risk btn-sm"
                  style={{ flexShrink: 0 }}
                  onClick={() => setShowProceed(true)}
                >
                  🔶 Open Anyway
                </button>
              </div>
            )}

            {/* MALICIOUS — blocked, no open button */}
            {result.isMalicious() && (
              <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
                <div style={{
                  width: 40, height: 40, borderRadius: 10, flexShrink: 0,
                  background: 'rgba(232,50,90,0.12)',
                  display: 'flex', alignItems: 'center', justifyContent: 'center',
                  fontSize: 20,
                }}>🚫</div>
                <div>
                  <div className="label" style={{ marginBottom: 2, color: 'var(--danger)' }}>
                    Link Blocked
                  </div>
                  <p style={{ fontSize: 13, color: 'var(--text-muted)' }}>
                    This link has been identified as malicious. Opening it has been disabled for your safety.
                  </p>
                </div>
              </div>
            )}

          </div>
        )}

        {/* Analysis details card */}
        <div className="card mt-4">
          <div className="label">Analysis Details</div>
          {[
            ['📋', 'Payload type',   payloadType.toUpperCase(), null],
            ['💬', 'Explanation',    result.explanation,        null],
            ['✔️', 'Recommendation', result.recommendation,     result.getRiskColor()],
          ].map(([icon, label, val, color]) => (
            <div key={label} className="detail-row">
              <span className="detail-icon">{icon}</span>
              <div>
                <div className="detail-label">{label}</div>
                <div className="detail-val" style={color ? { color, fontWeight: 500 } : {}}>
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
                {result.sources.map(s => <span className="chip" key={s}>{s}</span>)}
              </div>
            </div>
          </div>
        </div>

        {/* Sandbox preview — suspicious/high risk/malicious only */}
        {(result.isSuspicious() || result.isHighRisk() || result.isMalicious()) && isUrl && (
          <div className="card mt-4">
            <div className="flex-between" style={{ marginBottom: sandboxOpen ? 12 : 0 }}>
              <div>
                <div className="label" style={{ marginBottom: 2 }}>Sandbox Preview</div>
                <p style={{ fontSize: 13, color: 'var(--text-muted)' }}>
                  Inspect this URL safely in an isolated environment before deciding.
                </p>
              </div>
              {!sandboxOpen && (
                <button className="btn btn-warn btn-sm" onClick={() => setShowSandbox(true)}>
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

        {/* Anonymous feedback — report incorrect classification */}
        <FeedbackSection
          scanId={record.scanId}
          url={payload}
          onSubmit={(data) => feedbackSvc.submitFeedback(data)}
        />

        {/* Bottom action bar */}
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
