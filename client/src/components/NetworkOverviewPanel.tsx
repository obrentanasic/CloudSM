import { useCallback, useEffect, useState } from 'react';
import { api } from '../api/client';
import type { AlertLog, InvoiceStatistics, MeterNetworkStatus, PaymentRecord } from '../types';
import { AlertSeverityLabel, AlertTypeLabel } from '../types';

const money = (value: number) => value.toLocaleString('sr-Latn', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const kwh = (value: number) => value.toLocaleString('sr-Latn', { maximumFractionDigits: 2 });

const connectionLabel = (t: number) => (t === 1 ? 'Трофазно' : 'Монофазно');
const pairingLabel = (s: number) => (s === 1 ? 'упарено' : 'неупарено');
const invoiceStatusLabel = (s?: number | null) =>
  s === null || s === undefined ? 'нема рачуна' : s === 1 ? 'плаћен' : 'неплаћен';

export function NetworkOverviewPanel() {
  const [meters, setMeters] = useState<MeterNetworkStatus[]>([]);
  const [payments, setPayments] = useState<PaymentRecord[]>([]);
  const [stats, setStats] = useState<InvoiceStatistics | null>(null);
  const [alerts, setAlerts] = useState<AlertLog[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loaded, setLoaded] = useState(false);

  const load = useCallback(async () => {
    const [m, p, s, a] = await Promise.all([
      api.get<MeterNetworkStatus[]>('/api/admin/network/meters'),
      api.get<PaymentRecord[]>('/api/admin/network/payments?take=50'),
      api.get<InvoiceStatistics>('/api/admin/network/invoice-stats'),
      api.get<AlertLog[]>('/api/admin/network/alerts?take=50'),
    ]);
    setMeters(m);
    setPayments(p);
    setStats(s);
    setAlerts(a);
  }, []);

  useEffect(() => {
    load().catch((e) => setError((e as Error).message)).finally(() => setLoaded(true));
  }, [load]);

  if (!loaded) return null;

  const onlineCount = meters.filter((m) => m.isOnline).length;

  return (
    <div className="network-overview">
      {error && <div className="error banner">{error}</div>}

      {stats && (
        <section className="card stats-cards">
          <div className="stat-tile">
            <span className="stat-value">{onlineCount}/{meters.length}</span>
            <span className="muted small">бројила online</span>
          </div>
          <div className="stat-tile">
            <span className="stat-value">{stats.totalInvoices}</span>
            <span className="muted small">генерисано рачуна</span>
          </div>
          <div className="stat-tile">
            <span className="stat-value">{stats.emailsSent}/{stats.totalInvoices}</span>
            <span className="muted small">успешно послато мејлом</span>
          </div>
          <div className="stat-tile">
            <span className="stat-value">{stats.paidInvoices}/{stats.totalInvoices}</span>
            <span className="muted small">плаћено</span>
          </div>
          <div className="stat-tile">
            <span className="stat-value">{money(stats.totalAmountPaidRsd)}</span>
            <span className="muted small">наплаћено РСД</span>
          </div>
          <div className="stat-tile">
            <span className="stat-value">{money(stats.totalAmountUnpaidRsd)}</span>
            <span className="muted small">неплаћено РСД</span>
          </div>
        </section>
      )}

      <section className="card">
        <div className="row between">
          <h2>Статус мреже</h2>
          <span className="muted small">{meters.length} бројила</span>
        </div>
        <div className="network-table">
          <div className="network-table-head">
            <span /><span>Бројило</span><span>Власник</span><span>Објекат</span>
            <span>Потрошња (мес.)</span><span>Последњи рачун</span>
          </div>
          {meters.map((m) => (
            <div className="network-table-row" key={m.meterId}>
              <span className={`dot ${m.isOnline ? 'on' : 'off'}`} title={m.isOnline ? 'online' : 'offline'} />
              <span>
                <strong>{m.serialNumber}</strong>
                <div className="muted small">{connectionLabel(m.connectionType)} · {pairingLabel(m.pairingStatus)}</div>
              </span>
              <span>{m.ownerName}</span>
              <span>{m.propertyName}</span>
              <span>{kwh(m.monthConsumptionKwh)} kWh</span>
              <span className={m.lastInvoiceStatus === 1 ? 'badge paid' : m.lastInvoiceStatus === 0 ? 'badge unpaid' : 'badge'}>
                {invoiceStatusLabel(m.lastInvoiceStatus)}
              </span>
            </div>
          ))}
          {meters.length === 0 && <p className="muted small">Нема регистрованих бројила.</p>}
        </div>
      </section>

      <section className="card">
        <div className="row between">
          <h2>Преглед уплата</h2>
          <span className="muted small">{payments.length} последњих</span>
        </div>
        <div className="payment-list">
          {payments.map((p) => (
            <div className="payment-row" key={p.invoiceId}>
              <div>
                <strong>{p.consumerName}</strong>
                <div className="muted small">{p.serialNumber} · {p.year}-{String(p.month).padStart(2, '0')}</div>
              </div>
              <span>{money(p.totalAmountRsd)} РСД</span>
              <span className="muted small">{p.paidAtUtc ? new Date(p.paidAtUtc).toLocaleString('sr-RS') : '—'}</span>
            </div>
          ))}
          {payments.length === 0 && <p className="muted small">Још нема реализованих уплата.</p>}
        </div>
      </section>

      <section className="card">
        <div className="row between">
          <h2>Упозорења</h2>
          <span className="muted small">{alerts.length} последњих</span>
        </div>
        <div className="alert-list">
          {alerts.map((a) => (
            <div className={a.severity === 1 ? 'alert-row critical' : 'alert-row'} key={a.id}>
              <span className={a.severity === 1 ? 'badge danger' : 'badge'}>{AlertSeverityLabel(a.severity)}</span>
              <div>
                <strong>{AlertTypeLabel(a.type)}</strong> · {a.serialNumber}
                <div className="muted small">{a.message}</div>
              </div>
              <span className="muted small">{new Date(a.occurredAtUtc).toLocaleString('sr-RS')}</span>
              <span className={a.emailSent ? 'badge paid' : 'badge unpaid'}>{a.emailSent ? 'мејл послат' : 'без мејла'}</span>
            </div>
          ))}
          {alerts.length === 0 && <p className="muted small">Нема забележених упозорења.</p>}
        </div>
      </section>
    </div>
  );
}
