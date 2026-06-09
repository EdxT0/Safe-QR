/**
 * QRScannerService — Singleton
 * Manages QR code decoding from both live camera streams (WebRTC)
 * and uploaded image files. In production the decoding is handled
 * by the html5-qrcode library or ZXing.
 */
export class QRScannerService {
  static #instance = null;

  static getInstance() {
    if (!QRScannerService.#instance) {
      QRScannerService.#instance = new QRScannerService();
    }
    return QRScannerService.#instance;
  }

  /**
   * Requests camera access and attaches the stream to a <video> element.
   * @param {React.RefObject} videoRef
   * @returns {Promise<MediaStream>}
   */
  async startCameraScanner(videoRef) {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: 'environment' },
      });
      if (videoRef?.current) {
        videoRef.current.srcObject = stream;
        await videoRef.current.play();
      }
      return stream;
    } catch {
      throw new Error(
        'Camera access denied or unavailable. Please enable camera permissions or use Image Upload instead.'
      );
    }
  }

  /**
   * Stops all tracks on an active camera stream.
   * @param {MediaStream|null} stream
   */
  stopCameraScanner(stream) {
    stream?.getTracks().forEach(t => t.stop());
  }

  /**
   * Simulates decoding a QR code from the live camera feed.
   * TODO: replace with html5-qrcode Html5QrcodeScanner.
   * @returns {Promise<string>} decoded payload
   */
  async decodeFromCamera() {
    await new Promise(r => setTimeout(r, 1800));
    const demos = [
      'https://www.dbs.com.sg/personal/default.page',
      'http://free-prize-claim.xyz/win?ref=qr123',
      'WIFI:T:WPA;S:SafeQR-Office;P:securepass123;;',
    ];
    return demos[Math.floor(Math.random() * demos.length)];
  }

  /**
   * Simulates decoding a QR code from an uploaded image file.
   * TODO: replace with html5-qrcode Html5Qrcode.scanFile().
   * @param {File} file
   * @returns {Promise<string>} decoded payload
   */
  async decodeFromFile(file) {
    await new Promise(r => setTimeout(r, 700));
    if (!file) throw new Error('No file provided.');
    if (!/\.(png|jpg|jpeg)$/i.test(file.name)) {
      throw new Error('Please upload a PNG, JPG, or JPEG image.');
    }
    const n = file.name.toLowerCase();
    if (n.includes('bad') || n.includes('malicious')) return 'http://phishing-example-malware.tk/steal';
    if (n.includes('susp'))  return 'http://bit.ly/3xR9mQ2';
    if (n.includes('wifi'))  return 'WIFI:T:WPA;S:SafeQR-Office;P:securepass123;;';
    return 'https://www.sim.edu.sg';
  }
}
