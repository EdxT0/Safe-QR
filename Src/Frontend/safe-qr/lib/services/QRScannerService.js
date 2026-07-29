/**
 * QRScannerService — Singleton
 * Decodes QR codes from a live camera feed or an uploaded image file
 * using html5-qrcode (ZXing under the hood).
 */
import { Html5Qrcode } from 'html5-qrcode';

function toFriendlyCameraError(err) {
  const message = String(err?.message || err || '');
  if (/permission|notallowed/i.test(message)) {
    return 'Camera access denied or unavailable. Please enable camera permissions or use Image Upload instead.';
  }
  if (/notfound/i.test(message)) {
    return 'No camera was found on this device. Please use Image Upload instead.';
  }
  return 'Could not start the camera. Please use Image Upload instead.';
}

export class QRScannerService {
  static #instance = null;
  #html5QrCode = null;
  #decoded     = false;

  static getInstance() {
    if (!QRScannerService.#instance) {
      QRScannerService.#instance = new QRScannerService();
    }
    return QRScannerService.#instance;
  }

  /**
   * Starts a live camera QR scan inside the given container element.
   * Resolves once the camera preview is running; onDecoded fires exactly
   * once, after the camera has already been stopped, when a QR code is found.
   * @param {string} elementId - id of an empty container element already in the DOM
   * @param {(text: string) => void} onDecoded
   */
  async startCameraScan(elementId, onDecoded) {
    this.#decoded = false;
    this.#html5QrCode = new Html5Qrcode(elementId);

    try {
      await this.#html5QrCode.start(
        { facingMode: 'environment' },
        { fps: 10, qrbox: 240 },
        (decodedText) => {
          if (this.#decoded) return;
          this.#decoded = true;
          this.stopCameraScan().finally(() => onDecoded(decodedText));
        },
        () => {
          // Fired once per frame with no QR code found — expected, ignore.
        }
      );
    } catch (err) {
      this.#html5QrCode = null;
      throw new Error(toFriendlyCameraError(err));
    }
  }

  /** Stops and tears down the active camera scan, if any. */
  async stopCameraScan() {
    const instance = this.#html5QrCode;
    this.#html5QrCode = null;
    if (!instance) return;
    try {
      if (instance.isScanning) {
        await instance.stop();
      }
      instance.clear();
    } catch {
      // Already stopped/torn down — nothing more to do.
    }
  }

  /**
   * Decodes a QR code from an uploaded image file.
   * @param {File} file
   * @returns {Promise<string>} decoded payload
   */
  async decodeFromFile(file) {
    if (!file) throw new Error('No file provided.');
    if (!/\.(png|jpg|jpeg)$/i.test(file.name)) {
      throw new Error('Please upload a PNG, JPG, or JPEG image.');
    }

    const elementId = 'qr-file-scan-region';
    let container = document.getElementById(elementId);
    const createdContainer = !container;
    if (createdContainer) {
      container = document.createElement('div');
      container.id = elementId;
      container.style.display = 'none';
      document.body.appendChild(container);
    }

    const html5QrCode = new Html5Qrcode(elementId);
    try {
      return await html5QrCode.scanFile(file, /* showImage */ false);
    } catch (err) {
      console.error('QR decode failed:', err);
      throw new Error('Could not detect a QR code in this image. Please try a clearer photo.');
    } finally {
      html5QrCode.clear();
      if (createdContainer) container.remove();
    }
  }
}
