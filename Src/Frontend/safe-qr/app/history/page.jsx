'use client';
/**
 * HistoryPage (/history)
 * Displays the user's scan history with filtering, searching, and deletion.
 */
import { useState, useEffect } from 'react';
import { useRouter }           from 'next/navigation';
import { Navbar, ScanHistoryCard, DeleteModal, EmptyState, Spinner } from '../../components';
import { ScanHistoryService }  from '../../lib/services/ScanHistoryService';

export default function HistoryPage() {
  const router = useRouter();

  const [records,  setRecords]  = useState([]);
  const [loading,  setLoading]  = useState(true);
  const [query,    setQuery]    = useState('');
  const [filter,   setFilter]   = useState('all');
  const [deleteId, setDeleteId] = useState(null);

  const historySvc = ScanHistoryService.getInstance();

  const loadRecords = async () => {
    setLoading(true);
    const all = await historySvc.getAll();
    setRecords(all);
    setLoading(false);
  };

  useEffect(() => { loadRecords(); }, []);

  const confirmDelete = async () => {
    await historySvc.delete(deleteId);
    setDeleteId(null);
    loadRecords();
  };

  // Filtered + searched records
  const filtered = records.filter(r => {
    const matchQ = r.payload.toLowerCase().includes(query.toLowerCase()) ||
                   r.threatResult.riskLevel.includes(query.toLowerCase());
    const matchF = filter === 'all' || r.threatResult.riskLevel === filter;
    return matchQ && matchF;
  });

  const stats = {
    safe:       records.filter(r => r.threatResult.isSafe()).length,
    suspicious: records.filter(r => r.threatResult.isSuspicious()).length,
    malicious:  records.filter(r => r.threatResult.isMalicious()).length,
  };

  const filterTabs = ['all', 'safe', 'suspicious', 'malicious'];

  return (
    <>
      <Navbar activePath="/history" user={null} onLogout={() => {}} />
      <main className="page">
        <div className="page-title">📋 Scan History</div>
        <div className="page-sub">
          Review, search, and manage your previous QR scan records.
        </div>

        {deleteId && (
          <DeleteModal
            onConfirm={confirmDelete}
            onCancel={() => setDeleteId(null)}
          />
        )}

        {/* Stats cards */}
        <div className="stats-grid">
          {[
            ['✅', 'Safe',       stats.safe,       'safe'],
            ['⚠️', 'Suspicious', stats.suspicious, 'suspicious'],
            ['🚫', 'Malicious',  stats.malicious,  'malicious'],
          ].map(([icon, label, count, lvl]) => (
            <div
              key={lvl}
              className={`stat-card ${filter === lvl ? 'active-filter' : ''}`}
              onClick={() => setFilter(f => f === lvl ? 'all' : lvl)}
            >
              <div style={{ fontSize: 24 }}>{icon}</div>
              <div style={{ fontSize: 26, fontWeight: 700, marginTop: 4 }}>{count}</div>
              <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>{label}</div>
            </div>
          ))}
        </div>

        {/* Search */}
        <div className="search-bar">
          <span className="search-icon">🔍</span>
          <input
            className="input"
            placeholder="Search by URL or risk level…"
            value={query}
            onChange={e => setQuery(e.target.value)}
          />
        </div>

        {/* Filter tabs */}
        <div className="tabs">
          {filterTabs.map(f => (
            <button
              key={f}
              className={`tab ${filter === f ? 'active' : ''}`}
              onClick={() => setFilter(f)}
            >
              {f.charAt(0).toUpperCase() + f.slice(1)}
            </button>
          ))}
        </div>

        {/* Record list */}
        {loading ? (
          <div style={{ textAlign: 'center', padding: 48 }}><Spinner /></div>
        ) : filtered.length === 0 ? (
          <EmptyState
            icon="📂"
            title="No records found"
            subtitle={query
              ? 'Try a different search term.'
              : 'Scan a QR code to start building your history.'}
          />
        ) : (
          filtered.map(r => (
            <ScanHistoryCard
              key={r.scanId}
              record={r}
              onDelete={setDeleteId}
            />
          ))
        )}

        {/* Scan more CTA */}
        {!loading && records.length === 0 && (
          <div style={{ textAlign: 'center', marginTop: 24 }}>
            <button className="btn btn-primary" onClick={() => router.push('/scanner')}>
              Start Scanning
            </button>
          </div>
        )}
      </main>
    </>
  );
}
