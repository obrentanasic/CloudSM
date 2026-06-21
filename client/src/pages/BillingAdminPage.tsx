import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { AuthImage } from '../components/AuthImage';
import { NetworkOverviewPanel } from '../components/NetworkOverviewPanel';
import { UserManagementPanel } from '../components/UserManagementPanel';
import type { GeneratedInvoices, ManualReadingDto, TariffModel } from '../types';

const defaults = {
  name: 'Основни тарифни модел',
  greenLimitKwh: '350',
  blueLimitKwh: '1200',
  greenHighPriceRsd: '10.0',
  greenLowPriceRsd: '2.5',
  blueHighPriceRsd: '15.0',
  blueLowPriceRsd: '3.75',
  redHighPriceRsd: '30.0',
  redLowPriceRsd: '7.5',
  powerPriceRsdPerKw: '50.0',
  supplierFeeRsd: '150.0',
};

export function BillingAdminPage() {
  const { user, logout } = useAuth();
  const [tariffs, setTariffs] = useState<TariffModel[]>([]);
  const [form, setForm] = useState(defaults);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<GeneratedInvoices | null>(null);
  const [pendingReadings, setPendingReadings] = useState<ManualReadingDto[]>([]);
  const [reviewingId, setReviewingId] = useState<string | null>(null);
  const [activatingId, setActivatingId] = useState<string | null>(null);
  const [view, setView] = useState<'billing' | 'network' | 'users'>('billing');
  const isAdmin = user?.role === 'Admin';

  const previousMonth = useMemo(() => {
    const d = new Date();
    d.setMonth(d.getMonth() - 1);
    return { year: d.getFullYear(), month: d.getMonth() + 1 };
  }, []);
  const [period, setPeriod] = useState(previousMonth);

  const loadTariffs = useCallback(async () => {
    const data = await api.get<TariffModel[]>('/api/billing/tariffs');
    setTariffs(data);
  }, []);

  const loadPendingReadings = useCallback(async () => {
    const data = await api.get<ManualReadingDto[]>('/api/manual-readings/pending');
    setPendingReadings(data);
  }, []);

  useEffect(() => {
    loadTariffs().catch((e) => setError((e as Error).message));
    loadPendingReadings().catch((e) => setError((e as Error).message));
  }, [loadTariffs, loadPendingReadings]);

  async function createTariff(e: FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      await api.post('/api/billing/tariffs?activate=true', {
        name: form.name,
        greenLimitKwh: Number(form.greenLimitKwh),
        blueLimitKwh: Number(form.blueLimitKwh),
        greenHighPriceRsd: Number(form.greenHighPriceRsd),
        greenLowPriceRsd: Number(form.greenLowPriceRsd),
        blueHighPriceRsd: Number(form.blueHighPriceRsd),
        blueLowPriceRsd: Number(form.blueLowPriceRsd),
        redHighPriceRsd: Number(form.redHighPriceRsd),
        redLowPriceRsd: Number(form.redLowPriceRsd),
        powerPriceRsdPerKw: Number(form.powerPriceRsdPerKw),
        supplierFeeRsd: Number(form.supplierFeeRsd),
      });
      await loadTariffs();
    } catch (err) {
      setError((err as Error).message);
    }
  }

  async function activate(id: string) {
    setError(null);
    setActivatingId(id);
    try {
      await api.post(`/api/billing/tariffs/${id}/activate`);
      await loadTariffs();
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setActivatingId(null);
    }
  }

  async function generate(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setResult(null);
    try {
      const data = await api.post<GeneratedInvoices>('/api/billing/generate', period);
      setResult(data);
    } catch (err) {
      setError((err as Error).message);
    }
  }

  async function approveReading(id: string) {
    setError(null);
    setReviewingId(id);
    try {
      await api.post(`/api/manual-readings/${id}/approve`, { reviewNote: null });
      await loadPendingReadings();
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setReviewingId(null);
    }
  }

  async function rejectReading(id: string) {
    setError(null);
    const reviewNote = window.prompt('Разлог одбијања (опционо):') ?? null;
    setReviewingId(id);
    try {
      await api.post(`/api/manual-readings/${id}/reject`, { reviewNote });
      await loadPendingReadings();
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setReviewingId(null);
    }
  }

  return (
    <div className="app">
      <header className="topbar">
        <strong>Smart Metering</strong>
        <div className="spacer" />
        <span className="muted">{user?.fullName}</span>
        <button onClick={logout}>Одјава</button>
      </header>

      {error && <div className="error banner">{error}</div>}

      <div className="tabs">
        <button className={view === 'billing' ? 'tab active' : 'tab'} onClick={() => setView('billing')}>Наплата</button>
        <button className={view === 'network' ? 'tab active' : 'tab'} onClick={() => setView('network')}>Мрежа и упозорења</button>
        {isAdmin && (
          <button className={view === 'users' ? 'tab active' : 'tab'} onClick={() => setView('users')}>Корисници</button>
        )}
      </div>

      {view === 'users' && isAdmin ? (
        <UserManagementPanel />
      ) : view === 'network' ? (
        <main className="content">
          <NetworkOverviewPanel />
        </main>
      ) : (
      <main className="content admin-grid">
        <section className="card tariff-card">
          <div className="row between">
            <h2>Тарифни модели</h2>
            <span className="muted small">{tariffs.length} модела</span>
          </div>
          <div className="tariff-list">
            {tariffs.map((tariff) => (
              <div className="tariff-row" key={tariff.id}>
                <div>
                  <strong>{tariff.name}</strong>
                  <div className="muted small">
                    зелена до {tariff.greenLimitKwh} kWh · плава до {tariff.blueLimitKwh} kWh
                  </div>
                  <div className="muted small">креиран {new Date(tariff.createdAtUtc).toLocaleString('sr-RS')}</div>
                </div>
                <span className={tariff.isActive ? 'badge paid' : 'badge'}>{tariff.isActive ? 'Активан' : 'Неактиван'}</span>
                {!tariff.isActive && (
                  <button className="link" disabled={activatingId === tariff.id} onClick={() => activate(tariff.id)}>
                    {activatingId === tariff.id ? 'Активирам…' : 'Активирај'}
                  </button>
                )}
              </div>
            ))}
            {tariffs.length === 0 && <p className="muted small">Нема дефинисаних тарифних модела.</p>}
          </div>
        </section>

        <section className="card tariff-card">
          <h2>Нови тарифни модел</h2>
          <form className="tariff-form" onSubmit={createTariff}>
            <label>Назив<input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required /></label>
            <label>Зелена зона до kWh<input type="number" step="any" value={form.greenLimitKwh} onChange={(e) => setForm({ ...form, greenLimitKwh: e.target.value })} required /></label>
            <label>Плава зона до kWh<input type="number" step="any" value={form.blueLimitKwh} onChange={(e) => setForm({ ...form, blueLimitKwh: e.target.value })} required /></label>
            <label>Зелена ВТ<input type="number" step="any" value={form.greenHighPriceRsd} onChange={(e) => setForm({ ...form, greenHighPriceRsd: e.target.value })} required /></label>
            <label>Зелена НТ<input type="number" step="any" value={form.greenLowPriceRsd} onChange={(e) => setForm({ ...form, greenLowPriceRsd: e.target.value })} required /></label>
            <label>Плава ВТ<input type="number" step="any" value={form.blueHighPriceRsd} onChange={(e) => setForm({ ...form, blueHighPriceRsd: e.target.value })} required /></label>
            <label>Плава НТ<input type="number" step="any" value={form.blueLowPriceRsd} onChange={(e) => setForm({ ...form, blueLowPriceRsd: e.target.value })} required /></label>
            <label>Црвена ВТ<input type="number" step="any" value={form.redHighPriceRsd} onChange={(e) => setForm({ ...form, redHighPriceRsd: e.target.value })} required /></label>
            <label>Црвена НТ<input type="number" step="any" value={form.redLowPriceRsd} onChange={(e) => setForm({ ...form, redLowPriceRsd: e.target.value })} required /></label>
            <label>Обрачунска снага<input type="number" step="any" value={form.powerPriceRsdPerKw} onChange={(e) => setForm({ ...form, powerPriceRsdPerKw: e.target.value })} required /></label>
            <label>Трошак снабдевача<input type="number" step="any" value={form.supplierFeeRsd} onChange={(e) => setForm({ ...form, supplierFeeRsd: e.target.value })} required /></label>
            <button type="submit">Сачувај и активирај</button>
          </form>
        </section>

        <section className="card tariff-card span-full">
          <h2>Ручни обрачун</h2>
          <form className="limit-form" onSubmit={generate}>
            <label>Година<input type="number" value={period.year} onChange={(e) => setPeriod({ ...period, year: Number(e.target.value) })} required /></label>
            <label>Месец<input type="number" min="1" max="12" value={period.month} onChange={(e) => setPeriod({ ...period, month: Number(e.target.value) })} required /></label>
            <button type="submit">Генериши рачуне</button>
          </form>
          {result && (
            <p className="muted small">
              Период {result.year}-{String(result.month).padStart(2, '0')}: креирано {result.created}, прескочено {result.skipped}.
            </p>
          )}
        </section>

        <section className="card tariff-card span-full">
          <div className="row between">
            <h2>Пријаве стања бројила на чекању</h2>
            <span className="muted small">{pendingReadings.length} на чекању</span>
          </div>
          <div className="pending-readings">
            {pendingReadings.map((r) => (
              <div className="pending-reading-card" key={r.id}>
                <AuthImage
                  path={r.optimizedImageUrl ?? r.originalImageUrl}
                  alt={`Бројило ${r.serialNumber}`}
                  className="pending-reading-photo"
                />
                <div>
                  <strong>{r.serialNumber}</strong> · пријављено {r.declaredTotalEnergyKwh.toLocaleString('sr-Latn')} kWh
                  <div className="muted small">{new Date(r.submittedAtUtc).toLocaleString('sr-RS')}</div>
                  {r.note && <div className="muted small">Напомена потрошача: „{r.note}”</div>}
                  <div className="pending-reading-actions">
                    <button disabled={reviewingId === r.id} onClick={() => approveReading(r.id)}>
                      {reviewingId === r.id ? 'Обрада…' : 'Одобри'}
                    </button>
                    <button className="btn-reject" disabled={reviewingId === r.id} onClick={() => rejectReading(r.id)}>
                      Одбиј
                    </button>
                  </div>
                </div>
              </div>
            ))}
            {pendingReadings.length === 0 && <p className="muted small">Нема пријава на чекању.</p>}
          </div>
        </section>
      </main>
      )}
    </div>
  );
}
