import '../styles/globals.css';

export const metadata = {
  title:       'Safe QR — Multi-Layered QR Security Scanner',
  description: 'Scan QR codes safely with machine learning threat detection, threat intelligence, and sandboxed preview.',
};

export default function RootLayout({ children }) {
  return (
    <html lang="en">
      <body>
        <div className="app">
          {children}
        </div>
      </body>
    </html>
  );
}
