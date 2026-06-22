import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('consumer@smartmetering.com');
  const [password, setPassword] = useState('');
  const [recoveryEmail, setRecoveryEmail] = useState('');
  const [recovering, setRecovering] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [recoveryBusy, setRecoveryBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setNotice(null);
    setBusy(true);
    try {
      await login(email, password);
      navigate('/');
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setBusy(false);
    }
  }

  async function requestPasswordReset() {
    const target = recoveryEmail.trim();
    if (!target) {
      setError('Унесите имејл адресу.');
      return;
    }

    setError(null);
    setNotice(null);
    setRecoveryBusy(true);
    try {
      await api.post('/api/auth/forgot-password', { email: target });
      setNotice('Ако налог постоји, послат је мејл са линком за ресетовање лозинке.');
      setRecovering(false);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setRecoveryBusy(false);
    }
  }

  return (
    <div className="centered">
      <form className="card auth-card" onSubmit={submit}>
        <h1>Smart Metering</h1>
        <p className="muted">Пријава на платформу</p>
        <label>Имејл
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </label>
        <label>Лозинка
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
        </label>
        {error && <div className="error">{error}</div>}
        {notice && <div className="banner success">{notice}</div>}
        <button type="submit" disabled={busy}>{busy ? 'Пријава…' : 'Пријави се'}</button>
        <button
          type="button"
          className="link"
          onClick={() => {
            setRecovering((value) => !value);
            setRecoveryEmail((value) => value || email);
            setError(null);
            setNotice(null);
          }}
        >
          Заборављена лозинка?
        </button>
        {recovering && (
          <div className="recovery-panel">
            <label>Имејл за опоравак
              <input type="email" value={recoveryEmail} onChange={(e) => setRecoveryEmail(e.target.value)} required />
            </label>
            <button type="button" disabled={recoveryBusy} onClick={() => void requestPasswordReset()}>
              {recoveryBusy ? 'Слање…' : 'Пошаљи линк'}
            </button>
          </div>
        )}
      </form>
    </div>
  );
}
