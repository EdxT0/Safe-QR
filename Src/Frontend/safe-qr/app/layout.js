import '../styles/globals.css';
import { UserProvider } from '../components/UserContext';

export const metadata = {
  title:       'Safe QR — Multi-Layered QR Security Scanner',
  description: 'Scan QR codes safely with machine learning threat detection, threat intelligence, and sandboxed preview.',
};

export default function RootLayout({ children }) {
  return (
    <html lang="en">
      <body>
        {/* UserProvider wraps all pages so Navbar and any page
            can read/write the current user without prop drilling */}
        <UserProvider>
          <div className="app">
            {children}
          </div>
        </UserProvider>
      </body>
    </html>
  );
}
