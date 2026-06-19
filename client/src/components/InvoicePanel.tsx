import { useEffect, useState } from 'react';
import { API_BASE, getToken } from '../api/client';
import { api } from '../api/client';
import type { InvoicePage, TelemetryHistory } from '../types';
import { TariffLabel } from '../types';
 
const money = (value: number) => value.toLocaleString('sr-Latn', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const kwh = (value: number) => value.toLocaleString('sr-Latn', { maximumFractionDigits: 3 });
 
function toInputDate(value: Date) {
  return value.toISOString().slice(0, 10);
}
 
interface CheckoutSessionDto {
  sessionId: string;
  url: string;
}
 
export function InvoicePanel({ propertyId, meterId }: { propertyId: string; meterId: string | null }) {
  const [page, setPage] = useState(1);
  const [data, setData] = useState<InvoicePage | null>(null);
  const [history, setHistory] = useState<TelemetryHistory | null>(null);
  const [from, setFrom] = useState(() => {
    const d = new Date();
    d.setDate(d.getDate() - 30);
    return toInputDate(d);
  });
  const [to, setTo] = useState(() => toInputDate(new Date()));
  const [error, setError] = useState<string | null>(null);
  const [payingId, setPayingId] = useState<string | null>(null);
 
  useEffect(() => {
    setPage(1);
  }, [propertyId, meterId, from, to]);
 
  useEffect(() => {
    if (!meterId || !from || !to) {
      setData(null);
      return;
    }
 
    let cancelled = false;
    const fromIso = new Date(`${from}T00:00:00.000Z`).toISOString();
    const toIso = new Date(`${to}T23:59:59.999Z`).toISOString();
    const params = new URLSearchParams({
      page: String(page),
      pageSize: '5',
      meterId,
      from: fromIso,
      to: toIso,
    });
 
    api.get<InvoicePage>(`/api/billing/properties/${propertyId}/invoices?${params.toString()}`)
      .then((response) => {
        if (!cancelled) {
          setData(response);
          setError(null);
        }
      })
      .catch((err) => { if (!cancelled) setError((err as Error).message); });
    return () => { cancelled = true; };
  }, [propertyId, meterId, page, from, to]);
 
  useEffect(() => {
    if (!meterId) {
      setHistory(null);
      return;
    }
 
    if (!from || !to) {
      return;
    }
 
    let cancelled = false;
    const fromIso = new Date(`${from}T00:00:00.000Z`).toISOString();
    const toIso = new Date(`${to}T23:59:59.999Z`).toISOString();
    api.get<TelemetryHistory>(`/api/meters/${meterId}/telemetry/history?from=${encodeURIComponent(fromIso)}&to=${encodeURIComponent(toIso)}&take=500`)
      .then((response) => {
        if (!cancelled) {
          setHistory(response);
          setError(null);
        }
      })
      .catch((err) => { if (!cancelled) setError((err as Error).message); });
    return () => { cancelled = true; };
  }, [meterId, from, to]);
 
  async function downloadPdf(id: string) {
    setError(null);
    const token = getToken();
    const res = await fetch(`${API_BASE}/api/billing/invoices/${id}/pdf`, {
      headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    });
 
    if (!res.ok) {
      setError(`PDF није доступан (HTTP ${res.status}).`);
      return;
    }
 
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `racun-${id}.pdf`;
    link.click();
    URL.revokeObjectURL(url);
  }
 
  async function handlePay(invoiceId: string) {
    setError(null);
    setPayingId(invoiceId);
    try {
      const session = await api.post<CheckoutSessionDto>(
        `/api/billing/invoices/${invoiceId}/checkout-session`,
        {}
      );
      // Redirect to Stripe Checkout
      window.location.href = session.url;
    } catch (err) {
      setError((err as Error).message);
      setPayingId(null);
    }
  }
 
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1;
 
  return (
    <section className="invoice-panel card">
      <div className="row between">
        <h3>Дигитални картон потрошње и рачуна</h3>
        {data && data.totalCount > 0 && <span className="muted small">{data.totalCount} укупно</span>}
      </div>
      {error && <div className="error">{error}</div>}
 
      <div className="history-filter">
        <label>
          Од
          <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
        </label>
        <label>
          До
          <input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
        </label>
        {history && <span className="muted small">{history.serialNumber} · {history.points.length} очитавања</span>}
      </div>
 
      {meterId && history && history.points.length > 0 && (
        <div className="reading-list">
          {history.points.map((point) => (
            <div className="reading-row" key={`${point.observationTime}-${point.totalEnergyKwh}`}>
              <span>{new Date(point.observationTime).toLocaleString('sr-RS')}</span>
              <b>{kwh(point.totalEnergyKwh)} kWh</b>
              <span>{point.currentLoadKw.toFixed(2)} kW</span>
              <span>{point.voltage != null ? `${point.voltage.toFixed(1)} V` : '—'}</span>
              <span>{TariffLabel(point.tariff)}</span>
            </div>
          ))}
        </div>
      )}
 
      {meterId && history && history.points.length === 0 && (
        <p className="muted small">Нема очитавања за изабрани период.</p>
      )}
 
      {!meterId && <p className="muted small">Изаберите бројило да видите очитавања.</p>}
 
      {meterId && data && data.items.length > 0 ? (
        <>
          <div className="invoice-list">
            {data.items.map((invoice) => (
              <div className="invoice-row" key={invoice.id}>
                <div>
                  <strong>{invoice.year}-{String(invoice.month).padStart(2, '0')}</strong>
                  <div className="muted small">{invoice.serialNumber} · {new Date(invoice.issuedAtUtc).toLocaleDateString('sr-RS')}</div>
                </div>
                <div className="invoice-metrics">
                  <span>ВТ <b>{kwh(invoice.highTariffKwh)} kWh</b></span>
                  <span>НТ <b>{kwh(invoice.lowTariffKwh)} kWh</b></span>
                  <span>З/П/Ц <b>{kwh(invoice.greenKwh)} / {kwh(invoice.blueKwh)} / {kwh(invoice.redKwh)}</b></span>
                </div>
                <div className="invoice-total">
                  <b>{money(invoice.totalAmountRsd)} РСД</b>
                  <span className={invoice.status === 1 ? 'badge paid' : 'badge unpaid'}>
                    {invoice.status === 1 ? 'Плаћен' : 'Неплаћен'}
                  </span>
                </div>
                <div className="invoice-actions">
                  <button className="link" onClick={() => downloadPdf(invoice.id)}>PDF</button>
                  {invoice.status === 0 && (
                    <button
                      className="btn-pay"
                      disabled={payingId === invoice.id}
                      onClick={() => handlePay(invoice.id)}
                    >
                      {payingId === invoice.id ? 'Учитавање...' : 'Плати'}
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
          <div className="pager">
            <button className="link" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>Претходна</button>
            <span className="muted small">{page} / {totalPages}</span>
            <button className="link" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>Следећа</button>
          </div>
        </>
      ) : meterId ? (
        <p className="muted small">Још нема генерисаних рачуна за изабрано бројило и период.</p>
      ) : null}
    </section>
  );
}
