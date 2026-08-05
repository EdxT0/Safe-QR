'use client';
import { useState }    from 'react';
import { useRouter }   from 'next/navigation';
import { Spinner }     from '../../components';
import { useUser }     from '../../components/UserContext';
import { AuthService } from '../../lib/services/AuthService';

export default function LoginPage() {
  const router       = useRouter();
  const { setUser }  = useUser();

  const [email,    setEmail]    = useState('');
  const [password, setPassword] = useState('');
  const [loading,  setLoading]  = useState(false);
  const [error,    setError]    = useState('');

  const handleLogin = async () => {
    setError(''); setLoading(true);
    try {
      const user = await AuthService.getInstance().login(email, password);
      setUser(user);
      router.push('/scanner');
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-wrap">
      <div className="card auth-card">
        <div className="auth-logo">
          <div className="auth-logo-icon"><img src="/Safe_QR.jpg" alt="Safe QR logo" /></div>
          <div className="auth-title">Welcome back</div>
          <div className="auth-sub">Sign in to access your scan history</div>
        </div>

        {error && <div className="error-msg">{error}</div>}

        <div className="field">
          <label htmlFor="email">Email</label>
          <input id="email" className="input" type="email" placeholder="you@example.com"
            value={email} onChange={e => setEmail(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && handleLogin()} />
        </div>

        <div className="field">
          <label htmlFor="password">Password</label>
          <input id="password" className="input" type="password" placeholder="••••••••"
            value={password} onChange={e => setPassword(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && handleLogin()} />
        </div>

        <button className="btn btn-primary btn-full mt-2" onClick={handleLogin} disabled={loading}>
          {loading ? <Spinner /> : 'Sign In'}
        </button>

        <button
          className="btn btn-secondary btn-full mt-2"
          onClick={() => router.push('/scanner')}
          disabled={loading}
        >
          ← Continue without signing in
        </button>

        <div className="auth-switch">
          Don&apos;t have an account?{' '}
          <button onClick={() => router.push('/register')}>Create one</button>
        </div>
      </div>
    </div>
  );
}
