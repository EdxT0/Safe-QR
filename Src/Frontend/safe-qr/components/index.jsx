'use client';
/**
 * Shared UI Components
 * Reusable stateless and stateful components used across all pages.
 */
import Link          from 'next/link';
import { useRouter } from 'next/navigation';
import { useUser }   from './UserContext';
import { AuthService } from '../lib/services/AuthService';

// ── Spinner ──────────────────────────────────────────────────────────────────
export function Spinner() {
  return <span className="spinner" />;
}

// ── RiskBadge ─────────────────────────────────────────────────────────────────
export function RiskBadge({ level }) {
  const labels = { safe: 'Safe', suspicious: 'Suspicious', malicious: 'Malicious' };
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
    safe: '✅', suspicious: '⚠️', malicious: '🚫',
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
  const router = useRouter();
  const { user, setUser } = useUser();

  const handleLogout = () => {
    AuthService.getInstance().logout();
    setUser(null);
    router.push('/scanner');
  };

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

      {/* Centre nav links */}
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

      {/* Right — user avatar or sign-in button */}
      <div className="nav-right">
        {user ? (
          <>
            <span style={{ fontSize: 13, color: 'var(--text-muted)' }}>
              {user.getFirstName()}
            </span>
            <div className="nav-avatar" onClick={handleLogout} title="Sign out">
              {user.getInitials()}
            </div>
          </>
        ) : (
          <Link href="/login" className="btn btn-secondary btn-sm">
            Sign In
          </Link>
        )}
      </div>
    </nav>
  );
}
