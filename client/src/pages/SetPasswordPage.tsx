import { useState, type FormEvent } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { api } from '../api/client';

/** Handles both account activation (set-password) and password reset, chosen via the `mode` prop. */
export function SetPasswordPage({ mode }: { mode: 'set' | 'reset' }) {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const token = params.get('token') ?? '';
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  const title = mode === 'set' ? 'Постављање лозинке' : 'Ресетовање лозинке';

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    if (password !== confirm) {
      setError('Лозинке се не подударају.');
      return;
    }
    try {
      if (mode === 'set') {
        await api.post('/api/auth/set-password', { token, password });
      } else {
        await api.post('/api/auth/reset-password', { token, newPassword: password });
      }
      setDone(true);
    } catch (err) {
      setError((err as Error).message);
    }
  }

  if (!token) {
    return <div className="centered"><div className="card auth-card"><div className="error">Недостаје токен у линку.</div></div></div>;
  }

  return (
    <div className="centered">
      <form className="card auth-card" onSubmit={submit}>
        <h1>{title}</h1>
        {done ? (
          <>
            <p>Лозинка је успешно постављена.</p>
            <button type="button" onClick={() => navigate('/login')}>Иди на пријаву</button>
          </>
        ) : (
          <>
            <label>Нова лозинка
              <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
            </label>
            <label>Потврди лозинку
              <input type="password" value={confirm} onChange={(e) => setConfirm(e.target.value)} required />
            </label>
            {error && <div className="error">{error}</div>}
            <button type="submit">Сачувај</button>
          </>
        )}
      </form>
    </div>
  );
}
