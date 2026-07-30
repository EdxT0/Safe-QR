'use client';
/**
 * AdminPage (/admin)
 * US-07: system analytics (scan volume, malicious rate, daily breakdown).
 * US-08: threat detection reports — every scanned URL's verdict, plus the
 * full user-submitted misclassification feedback list — each exportable
 * to CSV. Gated to role=admin; everyone else is bounced off this page.
 */
import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Navbar, Spinner, EmptyState } from '../../components';
import { useUser } from '../../components/UserContext';
import { AdminService } from '../../lib/services/AdminService';
import { downloadCsv } from '../../lib/csv';

const VERDICT_STYLE = {
  safe:       { color: 'var(--safe)',    label: 'Safe' },
  suspicious: { color: 'var(--warn)',    label: 'Suspicious' },
  highRisk:   { color: 'var(--warn)',    label: 'High Risk' },
  malicious:  { color: 'var(--danger)',  label: 'Malicious' },
};

function Verdict({ value }) {
  const style = VERDICT_STYLE[value];
  return <span style={{ color: style?.color, fontWeight: 500 }}>{style?.label || value || '—'}</span>;
}

function toISODate(date) {
  return date.toISOString().slice(0, 10);
}

function formatDate(value) {
  return new Date(value).toLocaleDateString('en-SG', { day: '2-digit', month: 'short', year: 'numeric' });
}

function truncate(text, max = 48) {
  return text && text.length > max ? text.slice(0, max) + '…' : text;
}

export default function AdminPage() {
  const router = useRouter();
  const { user, loading: userLoading } = useUser();
  const adminSvc = AdminService.getInstance();

  const [from, setFrom] = useState(() => toISODate(new Date(Date.now() - 30 * 86400000)));
  const [to,   setTo]   = useState(() => toISODate(new Date()));

  const [analytics,        setAnalytics]        = useState(null);
  const [analyticsLoading, setAnalyticsLoading] = useState(true);
  const [analyticsError,   setAnalyticsError]   = useState('');

  const [urlReports,     setUrlReports]     = useState([]);
  const [reportsLoading, setReportsLoading] = useState(true);
  const [reportsError,   setReportsError]   = useState('');

  const [feedback,        setFeedback]        = useState([]);
  const [feedbackLoading, setFeedbackLoading] = useState(true);
  const [feedbackError,   setFeedbackError]   = useState('');

  const loadAnalytics = async (f, t) => {
    setAnalyticsLoading(true);
    setAnalyticsError('');
    try {
      setAnalytics(await adminSvc.getThreatsAnalytics(f, t));
    } catch (e) {
      setAnalyticsError(e.message || 'Could not load analytics.');
    } finally {
      setAnalyticsLoading(false);
    }
  };

  const loadReports = async () => {
    setReportsLoading(true);
    setReportsError('');
    try {
      setUrlReports(await adminSvc.getAllUrlReports());
    } catch (e) {
      setReportsError(e.message || 'Could not load threat reports.');
    } finally {
      setReportsLoading(false);
    }
  };

  const loadFeedback = async () => {
    setFeedbackLoading(true);
    setFeedbackError('');
    try {
      setFeedback(await adminSvc.getAllFeedback());
    } catch (e) {
      setFeedbackError(e.message || 'Could not load feedback.');
    } finally {
      setFeedbackLoading(false);
    }
  };

  // Only admins get past this page — everyone else is bounced elsewhere.
  useEffect(() => {
    if (userLoading) return;
    if (!user) { router.replace('/login'); return; }
    if (!user.isAdmin()) { router.replace('/scanner'); return; }
    loadAnalytics(from, to);
    loadReports();
    loadFeedback();
  }, [userLoading, user]);

  if (userLoading || !user || !user.isAdmin()) {
    return (
      <>
        <Navbar activePath="/admin" />
        <main className="page" style={{ textAlign: 'center', paddingTop: 80 }}>
          <Spinner />
        </main>
      </>
    );
  }

  const exportReportsCsv = () => downloadCsv('threat-reports.csv', urlReports.map(r => ({
    Id:              r.id,
    Url:             r.url,
    Verdict:         r.results?.serviceResultEnum,
    FlaggedForWrong: r.flaggedForWrong,
    CreatedAt:       r.createdAt,
  })));

  const exportFeedbackCsv = () => downloadCsv('threat-feedback.csv', feedback.map(f => ({
    Id:                f.id,
    Payload:           f.payload,
    PayloadType:       f.payloadType,
    SystemVerdict:     f.systemClassification?.serviceResultEnum,
    ReportedRiskLevel: f.reportedRiskLevel,
    Comment:           f.comment || '',
    ReportedBy:        f.userId != null ? `User #${f.userId}` : 'Anonymous',
    CreatedAt:         f.createdAt,
  })));

  const maliciousPct = analytics && analytics.totalScanned > 0
    ? Math.round((analytics.maliciousCount / analytics.totalScanned) * 100)
    : 0;

  return (
    <>
      <Navbar activePath="/admin" />
      <main className="page">
        <div className="page-title">🛡 Admin Dashboard</div>
        <div className="page-sub">System analytics and threat detection reports.</div>

        {/* US-07: System Analytics */}
        <div className="card mt-4">
          <div className="label" style={{ marginBottom: 12 }}>📊 System Analytics</div>

          <div style={{ display: 'flex', gap: 10, alignItems: 'flex-end', marginBottom: 16, flexWrap: 'wrap' }}>
            <div className="field" style={{ marginBottom: 0 }}>
              <label htmlFor="from">From</label>
              <input id="from" type="date" className="input" value={from} onChange={e => setFrom(e.target.value)} />
            </div>
            <div className="field" style={{ marginBottom: 0 }}>
              <label htmlFor="to">To</label>
              <input id="to" type="date" className="input" value={to} onChange={e => setTo(e.target.value)} />
            </div>
            <button className="btn btn-secondary btn-sm" onClick={() => loadAnalytics(from, to)} disabled={analyticsLoading}>
              {analyticsLoading ? <Spinner /> : 'Apply'}
            </button>
          </div>

          {analyticsError && <div className="error-msg">{analyticsError}</div>}

          {analytics && !analyticsError && (
            <>
              <div className="stats-grid" style={{ marginBottom: 16 }}>
                <div className="stat-card">
                  <div style={{ fontSize: 24 }}>🔍</div>
                  <div style={{ fontSize: 26, fontWeight: 700, marginTop: 4 }}>{analytics.totalScanned}</div>
                  <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>Total Scanned</div>
                </div>
                <div className="stat-card">
                  <div style={{ fontSize: 24 }}>🚫</div>
                  <div style={{ fontSize: 26, fontWeight: 700, marginTop: 4 }}>{analytics.maliciousCount}</div>
                  <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>Malicious</div>
                </div>
                <div className="stat-card">
                  <div style={{ fontSize: 24 }}>📈</div>
                  <div style={{ fontSize: 26, fontWeight: 700, marginTop: 4 }}>{maliciousPct}%</div>
                  <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>Malicious Rate</div>
                </div>
              </div>

              {analytics.dailyBreakdowns.length === 0 ? (
                <p style={{ fontSize: 13, color: 'var(--text-muted)' }}>No scans in this date range.</p>
              ) : (
                <div style={{ overflowX: 'auto' }}>
                  <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
                    <thead>
                      <tr style={{ borderBottom: '1px solid var(--border)' }}>
                        <th style={{ textAlign: 'left', padding: '8px 6px', color: 'var(--text-muted)' }}>Date</th>
                        <th style={{ textAlign: 'left', padding: '8px 6px', color: 'var(--text-muted)' }}>Verdict</th>
                        <th style={{ textAlign: 'right', padding: '8px 6px', color: 'var(--text-muted)' }}>Count</th>
                      </tr>
                    </thead>
                    <tbody>
                      {analytics.dailyBreakdowns.map((b, i) => (
                        <tr key={i} style={{ borderBottom: '1px solid var(--border)' }}>
                          <td style={{ padding: '8px 6px' }}>{formatDate(b.date)}</td>
                          <td style={{ padding: '8px 6px' }}><Verdict value={b.result} /></td>
                          <td style={{ padding: '8px 6px', textAlign: 'right' }}>{b.count}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </>
          )}
        </div>

        {/* US-08a: Threat Detection Reports (every scanned URL) */}
        <div className="card mt-4">
          <div className="flex-between" style={{ marginBottom: 12 }}>
            <div className="label" style={{ marginBottom: 0 }}>🌐 Threat Detection Reports</div>
            <button className="btn btn-secondary btn-sm" onClick={exportReportsCsv} disabled={urlReports.length === 0}>
              ⬇️ Export CSV
            </button>
          </div>

          {reportsError && <div className="error-msg">{reportsError}</div>}

          {reportsLoading ? (
            <div style={{ textAlign: 'center', padding: 24 }}><Spinner /></div>
          ) : urlReports.length === 0 ? (
            !reportsError && <EmptyState icon="🌐" title="No scans yet" subtitle="Scanned URLs will show up here." />
          ) : (
            <div style={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
                <thead>
                  <tr style={{ borderBottom: '1px solid var(--border)' }}>
                    <th style={{ textAlign: 'left', padding: '8px 6px', color: 'var(--text-muted)' }}>URL</th>
                    <th style={{ textAlign: 'left', padding: '8px 6px', color: 'var(--text-muted)' }}>Verdict</th>
                    <th style={{ textAlign: 'left', padding: '8px 6px', color: 'var(--text-muted)' }}>Flagged</th>
                    <th style={{ textAlign: 'left', padding: '8px 6px', color: 'var(--text-muted)' }}>Scanned</th>
                  </tr>
                </thead>
                <tbody>
                  {urlReports.map(r => (
                    <tr key={r.id} style={{ borderBottom: '1px solid var(--border)' }}>
                      <td style={{ padding: '8px 6px', wordBreak: 'break-all' }} title={r.url}>{truncate(r.url)}</td>
                      <td style={{ padding: '8px 6px' }}><Verdict value={r.results?.serviceResultEnum} /></td>
                      <td style={{ padding: '8px 6px' }}>{r.flaggedForWrong ? '🚩' : '—'}</td>
                      <td style={{ padding: '8px 6px', color: 'var(--text-muted)' }}>{formatDate(r.createdAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {/* US-08b: User-submitted misclassification feedback */}
        <div className="card mt-4">
          <div className="flex-between" style={{ marginBottom: 12 }}>
            <div className="label" style={{ marginBottom: 0 }}>📝 User Feedback</div>
            <button className="btn btn-secondary btn-sm" onClick={exportFeedbackCsv} disabled={feedback.length === 0}>
              ⬇️ Export CSV
            </button>
          </div>

          {feedbackError && <div className="error-msg">{feedbackError}</div>}

          {feedbackLoading ? (
            <div style={{ textAlign: 'center', padding: 24 }}><Spinner /></div>
          ) : feedback.length === 0 ? (
            !feedbackError && <EmptyState icon="📝" title="No feedback yet" subtitle="User-reported misclassifications will show up here." />
          ) : (
            <div style={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
                <thead>
                  <tr style={{ borderBottom: '1px solid var(--border)' }}>
                    <th style={{ textAlign: 'left', padding: '8px 6px', color: 'var(--text-muted)' }}>Payload</th>
                    <th style={{ textAlign: 'left', padding: '8px 6px', color: 'var(--text-muted)' }}>System Said</th>
                    <th style={{ textAlign: 'left', padding: '8px 6px', color: 'var(--text-muted)' }}>User Says</th>
                    <th style={{ textAlign: 'left', padding: '8px 6px', color: 'var(--text-muted)' }}>Comment</th>
                    <th style={{ textAlign: 'left', padding: '8px 6px', color: 'var(--text-muted)' }}>Reported By</th>
                    <th style={{ textAlign: 'left', padding: '8px 6px', color: 'var(--text-muted)' }}>When</th>
                  </tr>
                </thead>
                <tbody>
                  {feedback.map(f => (
                    <tr key={f.id} style={{ borderBottom: '1px solid var(--border)' }}>
                      <td style={{ padding: '8px 6px', wordBreak: 'break-all' }} title={f.payload}>{truncate(f.payload, 36)}</td>
                      <td style={{ padding: '8px 6px' }}><Verdict value={f.systemClassification?.serviceResultEnum} /></td>
                      <td style={{ padding: '8px 6px' }}><Verdict value={f.reportedRiskLevel} /></td>
                      <td style={{ padding: '8px 6px', color: 'var(--text-muted)' }} title={f.comment || ''}>{truncate(f.comment, 36) || '—'}</td>
                      <td style={{ padding: '8px 6px' }}>{f.userId != null ? `User #${f.userId}` : 'Anonymous'}</td>
                      <td style={{ padding: '8px 6px', color: 'var(--text-muted)' }}>{formatDate(f.createdAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </main>
    </>
  );
}
