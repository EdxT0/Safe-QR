'use client';
import { useState }    from 'react';
import { useRouter }   from 'next/navigation';
import { Spinner }     from '../../components';
import { useUser }     from '../../components/UserContext';
import { AuthService } from '../../lib/services/AuthService';

export default function RegisterPage() {
  const router       = useRouter();
  const { setUser }  = useUser();

  const [fullName,  setFullName]  = useState('');
  const [email,     setEmail]     = useState('');
  const [password,  setPassword]  = useState('');
  const [loading,   setLoading]   = useState(false);
  const [error,     setError]     = useState('');

  const handleRegister = async () => {
    setError(''); setLoading(true);
    try {
      const user = await AuthService.getInstance().register(fullName, email, password);
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
          <div className="auth-logo-icon">Q</div>
          <div className="auth-title">Create account</div>
          <div className="auth-sub">Start scanning QR codes safely</div>
        </div>

        {error && <div className="error-msg">{error}</div>}

        <div className="field">
          <label htmlFor="name">Full Name</label>
          <input id="name" className="input" placeholder="Your full name"
            value={fullName} onChange={e => setFullName(e.target.value)} />
        </div>

        <div className="field">
          <label htmlFor="email">Email</label>
          <input id="email" className="input" type="email" placeholder="you@example.com"
            value={email} onChange={e => setEmail(e.target.value)} />
        </div>

        <div className="field">
          <label htmlFor="password">Password</label>
          <input id="password" className="input" type="password" placeholder="Min. 8 characters"
            value={password} onChange={e => setPassword(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && handleRegister()} />
        </div>

        <button className="btn btn-primary btn-full mt-2" onClick={handleRegister} disabled={loading}>
          {loading ? <Spinner /> : 'Create Account'}
        </button>

        <div className="auth-switch">
          Already have an account?{' '}
          <button onClick={() => router.push('/login')}>Sign in</button>
        </div>
      </div>
    </div>
  );
}
