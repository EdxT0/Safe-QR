'use client';
/**
 * Shared UI Components
 * Reusable stateless and stateful components used across all pages.
 * Authentication removed — Safe QR operates without user login.
 */
import Link from 'next/link';
import { useState } from 'react';

// ── Spinner ───────────────────────────────────────────────────────────────────
export function Spinner() {
  return <span className="spinner" />;
}

// ── RiskBadge ─────────────────────────────────────────────────────────────────
export function RiskBadge({ level }) {
  const labels = { safe: 'Safe', suspicious: 'Suspicious', high_risk: 'High Risk', malicious: 'Malicious' };
  return (
    <span className={`risk-badge ${level}`}>
      <span className="risk-dot" />
      {labels[level] || level}
    </span>
  );
}

// ── EmptyState ────────────────────────────────────────────────────────────────
export function EmptyState({ icon, title, subtitle }) {
  return (
    <div className="empty">
      <div className="empty-icon">{icon}</div>
      <h3>{title}</h3>
      <p>{subtitle}</p>
    </div>
  );
}

// ── WarningModal ──────────────────────────────────────────────────────────────
export function WarningModal({ url, onConfirm, onCancel }) {
  return (
    <div className="modal-overlay">
      <div className="modal">
        <div className="modal-title">⚠️ Open in Sandbox?</div>
        <div className="modal-body">
          You are about to preview this URL in an isolated sandbox environment.
          The sandbox prevents access to your browser storage, cookies, and device
          resources. Exercise caution even within the sandbox.
          <br /><br />
          <code style={{ fontFamily: 'var(--mono)', fontSize: 12,
            wordBreak: 'break-all', color: 'var(--warn)' }}>
            {url}
          </code>
        </div>
        <div className="modal-actions">
          <button className="btn btn-secondary btn-sm" onClick={onCancel}>Cancel</button>
          <button className="btn btn-warn btn-sm" onClick={onConfirm}>Open Sandbox</button>
        </div>
      </div>
    </div>
  );
}

// ── ProceedModal — shown before opening a suspicious or high-risk URL ────────
export function ProceedModal({ url, riskLevel = 'suspicious', onConfirm, onCancel }) {
  const isHighRisk = riskLevel === 'high_risk';
  const accentVar  = isHighRisk ? 'var(--high-risk)' : 'var(--warn)';
  const label      = isHighRisk ? 'High Risk' : 'Suspicious';
  const btnClass   = isHighRisk ? 'btn-high-risk' : 'btn-warn';

  return (
    <div className="modal-overlay">
      <div className="modal">
        <div className="modal-title">{isHighRisk ? '🔶' : '⚠️'} Proceed at Your Own Risk?</div>
        <div className="modal-body">
          This URL has been flagged as <strong style={{ color: accentVar }}>{label}</strong>.
          Opening it may expose you to potential threats. Safe QR cannot guarantee
          the safety of this destination.
          <br /><br />
          <code style={{ fontFamily: 'var(--mono)', fontSize: 12,
            wordBreak: 'break-all', color: accentVar }}>
            {url}
          </code>
          <br /><br />
          Do you still want to open this link?
        </div>
        <div className="modal-actions">
          <button className="btn btn-secondary btn-sm" onClick={onCancel}>
            Cancel — Stay Safe
          </button>
          <button className={`btn btn-sm ${btnClass}`} onClick={onConfirm}>
            I Understand, Proceed
          </button>
        </div>
      </div>
    </div>
  );
}

// ── DeleteModal ───────────────────────────────────────────────────────────────
export function DeleteModal({ onConfirm, onCancel }) {
  return (
    <div className="modal-overlay">
      <div className="modal">
        <div className="modal-title">Delete Record?</div>
        <div className="modal-body">
          This scan record will be permanently deleted. This action cannot be undone.
        </div>
        <div className="modal-actions">
          <button className="btn btn-secondary btn-sm" onClick={onCancel}>Cancel</button>
          <button className="btn btn-danger btn-sm" onClick={onConfirm}>Delete</button>
        </div>
      </div>
    </div>
  );
}

// ── ScanHistoryCard ───────────────────────────────────────────────────────────
export function ScanHistoryCard({ record, onDelete }) {
  const icons = {
    safe: '✅', suspicious: '⚠️', high_risk: '🔶', malicious: '🚫',
    wifi: '📶', email: '📧', sms: '💬', telephone: '📞',
  };
  const icon = icons[record.threatResult.riskLevel] || '🔗';

  return (
    <div className="hist-item">
      <span className="hist-icon">{icon}</span>
      <div style={{ flex: 1, minWidth: 0 }}>
        <div className="flex-between" style={{ marginBottom: 4 }}>
          <RiskBadge level={record.threatResult.riskLevel} />
          <span style={{ fontSize: 11, color: 'var(--text-dim)' }}>
            {record.getFormattedDate()}
          </span>
        </div>
        <div className="hist-payload">{record.getTruncatedPayload()}</div>
        <div className="hist-meta">
          {record.payloadType.toUpperCase()} · {record.threatResult.confidenceScore}% confidence
        </div>
      </div>
      <div className="hist-actions">
        <button className="btn btn-danger btn-sm" onClick={() => onDelete(record.scanId)}>
          🗑
        </button>
      </div>
    </div>
  );
}

// ── Navbar ────────────────────────────────────────────────────────────────────
export function Navbar({ activePath }) {
  const links = [
    { href: '/scanner', label: 'Scanner' },
    { href: '/upload',  label: 'Upload'  },
    { href: '/history', label: 'History' },
  ];

  return (
    <nav className="nav">
      {/* Logo */}
      <Link href="/scanner" className="nav-logo">
        <div className="nav-logo-icon">Q</div>
        <div className="nav-logo-text">Safe<span>QR</span></div>
      </Link>

      {/* Nav links */}
      <div className="nav-links">
        {links.map(l => (
          <Link
            key={l.href}
            href={l.href}
            className={`nav-link${activePath === l.href ? ' active' : ''}`}
          >
            {l.label}
          </Link>
        ))}
      </div>

      {/* Right spacer — kept for layout balance */}
      <div className="nav-right" />
    </nav>
  );
}

// ── FeedbackSection — anonymous misclassification reporting ──────────────────
export function FeedbackSection({ scanId, url, onSubmit }) {
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState('');
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState('');

  const options = [
    { value: 'safe',       label: 'Safe',       icon: '✅' },
    { value: 'suspicious', label: 'Suspicious', icon: '⚠️' },
    { value: 'high_risk',  label: 'High Risk',  icon: '🔶' },
    { value: 'malicious',  label: 'Malicious',  icon: '🚫' },
  ];

  const handleSubmit = async () => {
    setError('');
    if (!selected) {
      setError('Please select what you believe the correct classification should be.');
      return;
    }
    setSubmitting(true);
    try {
      await onSubmit({ scanId, url, reportedRiskLevel: selected, comment });
      setSubmitted(true);
    } catch (e) {
      setError(e.message);
    } finally {
      setSubmitting(false);
    }
  };

  if (submitted) {
    return (
      <div className="card mt-4">
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <div style={{
            width: 40, height: 40, borderRadius: 10, flexShrink: 0,
            background: 'rgba(0,200,150,0.12)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            fontSize: 20,
          }}>✅</div>
          <div>
            <div className="label" style={{ marginBottom: 2, color: 'var(--safe)' }}>
              Feedback Submitted
            </div>
            <p style={{ fontSize: 13, color: 'var(--text-muted)' }}>
              Thank you. Your feedback has been recorded anonymously and will help improve future classifications.
            </p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="card mt-4">
      <div className="flex-between" style={{ marginBottom: open ? 14 : 0 }}>
        <div>
          <div className="label" style={{ marginBottom: 2 }}>Report Incorrect Classification</div>
          <p style={{ fontSize: 13, color: 'var(--text-muted)' }}>
            Think this result is wrong? Let us know anonymously.
          </p>
        </div>
        {!open && (
          <button className="btn btn-secondary btn-sm" onClick={() => setOpen(true)}>
            🚩 Submit Feedback
          </button>
        )}
      </div>

      {open && (
        <>
          <div className="result-payload" style={{ marginTop: 0, marginBottom: 14 }}>
            {url}
          </div>

          <div className="field">
            <label>What do you believe the correct classification should be?</label>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2,1fr)', gap: 8 }}>
              {options.map(opt => (
                <button
                  key={opt.value}
                  className="btn btn-secondary btn-sm"
                  style={{
                    justifyContent: 'flex-start',
                    border: selected === opt.value
                      ? '1px solid var(--accent)'
                      : '1px solid var(--border-hi)',
                    background: selected === opt.value
                      ? 'rgba(0,229,160,0.08)'
                      : 'var(--bg-glass)',
                    color: selected === opt.value ? 'var(--accent)' : 'var(--text)',
                  }}
                  onClick={() => setSelected(opt.value)}
                >
                  {opt.icon} {opt.label}
                </button>
              ))}
            </div>
          </div>

          <div className="field">
            <label htmlFor="feedback-comment">Reason (optional)</label>
            <textarea
              id="feedback-comment"
              className="input"
              rows={3}
              placeholder="Tell us why you think this classification is incorrect…"
              value={comment}
              onChange={e => setComment(e.target.value)}
              style={{ resize: 'vertical', fontFamily: 'var(--font)' }}
            />
          </div>

          {error && <div className="error-msg">{error}</div>}

          <div style={{ display: 'flex', gap: 10 }}>
            <button
              className="btn btn-primary"
              style={{ flex: 1 }}
              onClick={handleSubmit}
              disabled={submitting}
            >
              {submitting ? <Spinner /> : 'Submit Feedback'}
            </button>
            <button
              className="btn btn-secondary"
              onClick={() => { setOpen(false); setError(''); setSelected(''); setComment(''); }}
              disabled={submitting}
            >
              Cancel
            </button>
          </div>

          <p style={{ fontSize: 11, color: 'var(--text-dim)', marginTop: 10 }}>
            Feedback is submitted anonymously and linked only to this scan record by its scanId.
            No personal information or device data is collected.
          </p>
        </>
      )}
    </div>
  );
}
