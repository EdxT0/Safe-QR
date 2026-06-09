'use client';
/**
 * UploadPage (/upload)
 * Allows the user to upload a QR code image for safety analysis.
 * Supports drag-and-drop and file browser selection.
 */
import { useState, useRef, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import { Navbar, Spinner }       from '../../components';
import { QRScannerService }      from '../../lib/services/QRScannerService';
import { ThreatAnalysisService } from '../../lib/services/ThreatAnalysisService';
import { ScanHistoryService }    from '../../lib/services/ScanHistoryService';
import { ScanRecord }            from '../../lib/models/ScanRecord';

export default function UploadPage() {
  const router = useRouter();

  const [dragging,  setDragging]  = useState(false);
  const [file,      setFile]      = useState(null);
  const [decoding,  setDecoding]  = useState(false);
  const [analysing, setAnalysing] = useState(false);
  const [error,     setError]     = useState('');

  const fileRef     = useRef(null);
  const scannerSvc  = QRScannerService.getInstance();
  const analysisSvc = ThreatAnalysisService.getInstance();
  const historySvc  = ScanHistoryService.getInstance();

  const handleFile = f => {
    setError('');
    setFile(f);
  };

  const processFile = async () => {
    if (!file) return;
    setError('');
    setDecoding(true);
    try {
      const payload  = await scannerSvc.decodeFromFile(file);
      setDecoding(false);
      setAnalysing(true);
      const type     = analysisSvc.detectPayloadType(payload);
      const result   = await analysisSvc.analysePayload(payload);
      const record   = new ScanRecord({ payload, payloadType: type, threatResult: result });
      await historySvc.save(record);
      sessionStorage.setItem('safeqr_result', JSON.stringify(record.toJSON()));
      router.push('/result');
    } catch (e) {
      setError(e.message);
      setDecoding(false);
      setAnalysing(false);
    }
  };

  const onDrop = useCallback(e => {
    e.preventDefault();
    setDragging(false);
    const f = e.dataTransfer?.files?.[0];
    if (f) handleFile(f);
  }, []);

  const steps = [
    ['📤', 'Upload',  'Select or drag a PNG / JPG / JPEG file containing a QR code'],
    ['🔍', 'Decode',  'The QR payload is extracted from the image'],
    ['🛡', 'Analyse', 'ML model and threat intelligence APIs classify the payload'],
    ['📊', 'Result',  'Your safety report is displayed instantly'],
  ];

  return (
    <>
      <Navbar activePath="/upload" user={null} onLogout={() => {}} />
      <main className="page">
        <div className="page-title">🖼 Image Upload</div>
        <div className="page-sub">
          Upload a QR code image or screenshot for instant safety analysis.
        </div>

        <div className="card">
          <div
            className={`scan-zone ${dragging ? 'drag' : ''}`}
            onDragOver={e => { e.preventDefault(); setDragging(true); }}
            onDragLeave={() => setDragging(false)}
            onDrop={onDrop}
            onClick={() => fileRef.current?.click()}
          >
            <span className="scan-zone-icon">{file ? '✅' : '📂'}</span>
            <h3>{file ? file.name : 'Drop your QR image here'}</h3>
            <p>
              {file
                ? `${(file.size / 1024).toFixed(1)} KB · Click to change file`
                : 'PNG, JPG, JPEG supported · or click to browse'}
            </p>
            <input
              ref={fileRef}
              type="file"
              accept=".png,.jpg,.jpeg"
              style={{ display: 'none' }}
              onChange={e => handleFile(e.target.files[0])}
            />
          </div>

          {error && <div className="error-msg" style={{ marginTop: 14 }}>{error}</div>}

          {file && (
            <button
              className="btn btn-primary btn-full mt-4"
              onClick={processFile}
              disabled={decoding || analysing}
            >
              {decoding  ? <><Spinner /> Decoding QR…</>    :
               analysing ? <><Spinner /> Analysing threat…</> :
               '🔍 Decode & Analyse'}
            </button>
          )}
        </div>

        {/* How it works */}
        <div className="card mt-4">
          <div className="label">How it works</div>
          {steps.map(([icon, title, desc]) => (
            <div key={title} className="detail-row">
              <span className="detail-icon">{icon}</span>
              <div>
                <div className="detail-label">{title}</div>
                <div className="detail-val" style={{ fontSize: 13, color: 'var(--text-muted)' }}>
                  {desc}
                </div>
              </div>
            </div>
          ))}
        </div>
      </main>
    </>
  );
}
