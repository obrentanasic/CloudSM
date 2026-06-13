import { useEffect, useState } from 'react';
import { API_BASE, getToken } from '../api/client';
import { api } from '../api/client';
import type { InvoicePage } from '../types';

const money = (value: number) => value.toLocaleString('sr-Latn', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const kwh = (value: number) => value.toLocaleString('sr-Latn', { maximumFractionDigits: 3 });

export function InvoicePanel({ propertyId }: { propertyId: string }) {
  const [page, setPage] = useState(1);
  const [data, setData] = useState<InvoicePage | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setPage(1);
  }, [propertyId]);

  useEffect(() => {
    let cancelled = false;
    api.get<InvoicePage>(`/api/billing/properties/${propertyId}/invoices?page=${page}&pageSize=5`)
      .then((response) => {
        if (!cancelled) {
          setData(response);
          setError(null);
        }
      })
      .catch((err) => { if (!cancelled) setError((err as Error).message); });
    return () => { cancelled = true; };
  }, [propertyId, page]);

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

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1;

  return (
    <section className="invoice-panel card">
      <div className="row between">
        <h3>Дигитални картон рачуна</h3>
        {data && data.totalCount > 0 && <span className="muted small">{data.totalCount} укупно</span>}
      </div>
      {error && <div className="error">{error}</div>}

      {data && data.items.length > 0 ? (
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
                <button className="link" onClick={() => downloadPdf(invoice.id)}>PDF</button>
              </div>
            ))}
          </div>
          <div className="pager">
            <button className="link" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>Претходна</button>
            <span className="muted small">{page} / {totalPages}</span>
            <button className="link" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>Следећа</button>
          </div>
        </>
      ) : (
        <p className="muted small">Још нема генерисаних рачуна за овај објекат.</p>
      )}
    </section>
  );
}
