import { useEffect, useState } from 'react';
import { api } from '../api/client';

type Status = 'confirming' | 'paid' | 'pending';

export function PaymentSuccessPage() {
  const [status, setStatus] = useState<Status>('confirming');

  useEffect(() => {
    const sessionId = new URLSearchParams(window.location.search).get('session_id');
    if (!sessionId) {
      setStatus('pending');
      return;
    }

    let cancelled = false;
    api.post<{ paid: boolean }>('/api/billing/payment-confirm', { sessionId })
      .then((r) => { if (!cancelled) setStatus(r.paid ? 'paid' : 'pending'); })
      .catch(() => { if (!cancelled) setStatus('pending'); });
    return () => { cancelled = true; };
  }, []);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', minHeight: '60vh', gap: '1rem' }}>
      {status === 'confirming' ? (
        <>
          <div style={{ fontSize: '3rem' }}>⏳</div>
          <h2>Потврђујем плаћање…</h2>
        </>
      ) : status === 'paid' ? (
        <>
          <div style={{ fontSize: '3rem' }}>✅</div>
          <h2>Плаћање успешно!</h2>
          <p className="muted">Рачун је означен као плаћен. Хвала вам.</p>
        </>
      ) : (
        <>
          <div style={{ fontSize: '3rem' }}>🕓</div>
          <h2>Плаћање примљено</h2>
          <p className="muted">Статус рачуна ће бити ажуриран убрзо.</p>
        </>
      )}
      <a href="/" className="btn-pay" style={{ textDecoration: 'none', padding: '0.5rem 1.5rem' }}>
        Назад на почетну
      </a>
    </div>
  );
}
