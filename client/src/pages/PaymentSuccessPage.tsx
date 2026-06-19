export function PaymentSuccessPage() {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', minHeight: '60vh', gap: '1rem' }}>
      <div style={{ fontSize: '3rem' }}>✅</div>
      <h2>Плаћање успешно!</h2>
      <p className="muted">Рачун је означен као плаћен. Хвала вам.</p>
      <a href="/" className="btn-pay" style={{ textDecoration: 'none', padding: '0.5rem 1.5rem' }}>
        Назад на почетну
      </a>
    </div>
  );
}