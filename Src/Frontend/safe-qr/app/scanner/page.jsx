'use client';
/**
 * ScannerPage (/scanner)
 * Allows the user to scan a QR code using the device camera.
 * Uses QRScannerService for camera access and ThreatAnalysisService for analysis.
 */
import { useState, useRef, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Navbar, Spinner }         from '../../components';
import { QRScannerService }        from '../../lib/services/QRScannerService';
import { ThreatAnalysisService }   from '../../lib/services/ThreatAnalysisService';
import { ScanHistoryService }      from '../../lib/services/ScanHistoryService';
import { ScanRecord }              from '../../lib/models/ScanRecord';

export default function ScannerPage() {
  const router = useRouter();

  const [cameraActive, setCameraActive] = useState(false);
  const [stream,       setStream]       = useState(null);
  const [starting,     setStarting]     = useState(false);
  const [analysing,    setAnalysing]    = useState(false);
  const [demoTarget,   setDemoTarget]   = useState('');
  const [cameraError,  setCameraError]  = useState('');

  const videoRef    = useRef(null);
  const scannerSvc  = QRScannerService.getInstance();
  const analysisSvc = ThreatAnalysisService.getInstance();
  const historySvc  = ScanHistoryService.getInstance();

  // Stop camera stream on unmount
  useEffect(() => () => scannerSvc.stopCameraScanner(stream), [stream]);

  const startCamera = async () => {
    setCameraError('');
    setStarting(true);
    try {
      const s = await scannerSvc.startCameraScanner(videoRef);
      setStream(s);
      setCameraActive(true);
    } catch (e) {
      setCameraError(e.message);
    } finally {
      setStarting(false);
    }
  };

  const stopCamera = () => {
    scannerSvc.stopCameraScanner(stream);
    setCameraActive(false);
    setStream(null);
  };

  const captureAndAnalyse = async (overridePayload = null) => {
    setAnalysing(true);
    try {
      const payload = overridePayload ?? await scannerSvc.decodeFromCamera();
      const type    = analysisSvc.detectPayloadType(payload);
      const result  = await analysisSvc.analysePayload(payload);
      const record  = new ScanRecord({ payload, payloadType: type, threatResult: result });
      await historySvc.save(record);
      stopCamera();
      // Pass result to result page via sessionStorage
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
    { label: '🔶 High Risk',  payload: 'https://paypa1-secure-login.com/verify',    badge: 'high_risk'  },
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
            <div
              className="scan-zone"
              onClick={startCamera}
              style={starting ? { pointerEvents: 'none', opacity: 0.6 } : {}}
            >
              <span className="scan-zone-icon">📷</span>
              <h3>Activate Camera Scanner</h3>
              <p>Click to start live QR scanning with your device camera</p>
              {starting && <div style={{ marginTop: 16 }}><Spinner /></div>}
            </div>
            {cameraError && <div className="error-msg" style={{ marginTop: 14 }}>{cameraError}</div>}
          </div>
        ) : (
          <div className="card">
            <div className="camera-box">
              <video ref={videoRef} autoPlay playsInline muted />
              <div className="scan-overlay">
                <div className="scan-frame">
                  <span />
                  <div className="scan-line" />
                </div>
              </div>
            </div>
            <div style={{ display: 'flex', gap: 10, marginTop: 16 }}>
              <button
                className="btn btn-primary"
                style={{ flex: 1 }}
                onClick={() => captureAndAnalyse()}
                disabled={analysing}
              >
                {analysing ? <><Spinner /> Analysing…</> : '⚡ Capture & Scan'}
              </button>
              <button className="btn btn-secondary" onClick={stopCamera}>Stop</button>
            </div>
            {cameraError && <div className="error-msg" style={{ marginTop: 12 }}>{cameraError}</div>}
          </div>
        )}

        {/* Demo payloads */}
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
                  captureAndAnalyse(payload);
                }}
              >
                {label}
                {analysing && demoTarget === payload && <Spinner />}
              </button>
            ))}
          </div>
        </div>

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
