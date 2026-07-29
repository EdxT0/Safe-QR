'use client';
/**
 * ScannerPage (/scanner)
 * Allows the user to scan a QR code using the device camera.
 * Uses QRScannerService for camera access and ThreatAnalysisService for analysis.
 */
import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Navbar, Spinner }         from '../../components';
import { QRScannerService }        from '../../lib/services/QRScannerService';
import { ThreatAnalysisService }   from '../../lib/services/ThreatAnalysisService';
import { ScanRecord }              from '../../lib/models/ScanRecord';

const CAMERA_REGION_ID = 'qr-camera-region';

export default function ScannerPage() {
  const router = useRouter();

  const [cameraActive, setCameraActive] = useState(false);
  const [starting,     setStarting]     = useState(false);
  const [analysing,    setAnalysing]    = useState(false);
  const [demoTarget,   setDemoTarget]   = useState('');
  const [cameraError,  setCameraError]  = useState('');

  const scannerSvc  = QRScannerService.getInstance();
  const analysisSvc = ThreatAnalysisService.getInstance();

  // Starts/stops the real camera scan whenever cameraActive flips —
  // the container div must be rendered (i.e. cameraActive already true)
  // before html5-qrcode can attach to it by element id.
  useEffect(() => {
    if (!cameraActive) return;
    let cancelled = false;
    setStarting(true);

    scannerSvc.startCameraScan(CAMERA_REGION_ID, (decodedText) => {
      if (cancelled) return;
      setCameraActive(false);
      analyseAndGo(decodedText);
    })
      .catch(e => { if (!cancelled) { setCameraError(e.message); setCameraActive(false); } })
      .finally(() => { if (!cancelled) setStarting(false); });

    return () => {
      cancelled = true;
      scannerSvc.stopCameraScan();
    };
  }, [cameraActive]);

  const startCamera = () => {
    setCameraError('');
    setCameraActive(true);
  };

  const stopCamera = () => {
    setCameraActive(false);
  };

  const analyseAndGo = async (payload) => {
    setAnalysing(true);
    try {
      const type   = analysisSvc.detectPayloadType(payload);
      const result = await analysisSvc.analysePayload(payload);
      const record = new ScanRecord({ payload, payloadType: type, threatResult: result });
      // Saving to history happens on the result page (single save path,
      // avoids double-saving the same scan to the backend).
      sessionStorage.setItem('safeqr_result', JSON.stringify(record.toJSON()));
      router.push('/result');
    } catch (e) {
      setCameraError(e.message);
    } finally {
      setAnalysing(false);
      setDemoTarget('');
    }
  };

  const demos = [
    { label: '🟢 Safe URL',   payload: 'https://www.google.com',                    badge: 'safe'       },
    { label: '🟡 Suspicious', payload: 'http://bit.ly/3xR9mQ2',                     badge: 'suspicious' },
    { label: '🔴 Malicious',  payload: 'http://phishing-example-malware.tk/steal',   badge: 'malicious'  },
    { label: '📶 Wi-Fi QR',   payload: 'WIFI:T:WPA;S:SafeQR-Office;P:securepass;;', badge: 'safe'       },
  ];

  return (
    <>
      <Navbar activePath="/scanner" />
      <main className="page">
        <div className="page-title">📷 QR Scanner</div>
        <div className="page-sub">
          Scan a QR code using your device camera for instant safety analysis.
        </div>

        {/* Camera section */}
        {!cameraActive ? (
          <div className="card">
            <div className="scan-zone" onClick={startCamera}>
              <span className="scan-zone-icon">📷</span>
              <h3>Activate Camera Scanner</h3>
              <p>Click to start live QR scanning with your device camera</p>
            </div>
            {cameraError && <div className="error-msg" style={{ marginTop: 14 }}>{cameraError}</div>}
          </div>
        ) : (
          <div className="card">
            {/* html5-qrcode owns this node's DOM directly (video/canvas it
                creates itself) — it must never contain React-rendered
                children, or React's reconciliation will conflict with it. */}
            <div style={{ position: 'relative' }}>
              <div className="camera-box" id={CAMERA_REGION_ID} />
              {starting && (
                <div style={{
                  position: 'absolute', inset: 0,
                  display: 'flex', alignItems: 'center', justifyContent: 'center',
                }}>
                  <Spinner />
                </div>
              )}
            </div>
            <p style={{ textAlign: 'center', fontSize: 13, color: 'var(--text-muted)', marginTop: 12 }}>
              {analysing
                ? <><Spinner /> Analysing detected code…</>
                : starting
                  ? 'Starting camera…'
                  : 'Point your camera at a QR code — it scans automatically.'}
            </p>
            <div style={{ display: 'flex', gap: 10, marginTop: 12 }}>
              <button className="btn btn-secondary" style={{ flex: 1 }} onClick={stopCamera} disabled={analysing}>
                Stop
              </button>
            </div>
            {cameraError && <div className="error-msg" style={{ marginTop: 12 }}>{cameraError}</div>}
          </div>
        )}

        {/* Demo payloads — hidden for production; uncomment to re-enable for testing/demos.
        <div className="card mt-4">
          <div className="label" style={{ marginBottom: 12 }}>
            Demo Payloads — simulate a scan result
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8 }}>
            {demos.map(({ label, payload, badge }) => (
              <button
                key={label}
                className="btn btn-secondary"
                style={{ justifyContent: 'flex-start', fontSize: 13 }}
                disabled={analysing}
                onClick={() => {
                  setDemoTarget(payload);
                  analyseAndGo(payload);
                }}
              >
                {label}
                {analysing && demoTarget === payload && <Spinner />}
              </button>
            ))}
          </div>
        </div>
        */}

        {/* Switch to upload */}
        <div className="card mt-4" style={{ textAlign: 'center' }}>
          <p style={{ fontSize: 13, color: 'var(--text-muted)', marginBottom: 14 }}>
            Prefer to upload an image instead?
          </p>
          <a href="/upload" className="btn btn-secondary">🖼 Switch to Image Upload</a>
        </div>
      </main>
    </>
  );
}
