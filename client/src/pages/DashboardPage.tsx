import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { useTelemetryHub } from '../hooks/useTelemetryHub';
import { TariffBarChart, VoltageLoadChart } from '../components/Charts';
import { LimitPanel } from '../components/LimitPanel';
import { InvoicePanel } from '../components/InvoicePanel';
import { ManualReadingPanel } from '../components/ManualReadingPanel';
import { BillingAdminPage } from './BillingAdminPage';
import type { Meter, MeterLiveUpdate, MeterStatus, Property, TelemetryPoint } from '../types';
import { TariffLabel } from '../types';

interface LiveView {
  meterId: string;
  serialNumber: string;
  load: number;
  voltage?: number | null;
  tariff: number;
  totalEnergy: number;
  online: boolean;
}

const fromStatus = (s: MeterStatus): LiveView => ({
  meterId: s.meterId,
  serialNumber: s.serialNumber,
  load: s.lastLoadKw,
  voltage: s.lastVoltage,
  tariff: s.currentTariff,
  totalEnergy: s.lastTotalEnergyKwh,
  online: s.isOnline,
});

const fromUpdate = (u: MeterLiveUpdate): LiveView => ({
  meterId: u.meterId,
  serialNumber: u.serialNumber,
  load: u.currentLoadKw,
  voltage: u.voltage,
  tariff: u.tariff,
  totalEnergy: u.totalEnergyKwh,
  online: true,
});

export function DashboardPage() {
  const { user } = useAuth();
  return user?.role === 'Consumer' ? <ConsumerDashboard /> : <BillingAdminPage />;
}

function ConsumerDashboard() {
  const { user, logout } = useAuth();
  const [properties, setProperties] = useState<Property[]>([]);
  const [activeId, setActiveId] = useState<string | null>(null);
  const [meters, setMeters] = useState<Meter[]>([]);
  const [statuses, setStatuses] = useState<Record<string, LiveView>>({});
  const [selectedMeterId, setSelectedMeterId] = useState<string | null>(null);
  const [points, setPoints] = useState<TelemetryPoint[]>([]);
  const [error, setError] = useState<string | null>(null);

  const loadProperties = useCallback(async () => {
    const data = await api.get<Property[]>('/api/properties');
    setProperties(data);
    setActiveId((cur) => (cur && data.some((p) => p.id === cur) ? cur : data[0]?.id ?? null));
  }, []);

  useEffect(() => {
    loadProperties().catch((e) => setError((e as Error).message));
  }, [loadProperties]);

  // Load meters + initial live snapshot when the active property changes.
  useEffect(() => {
    if (!activeId) return;
    let cancelled = false;
    (async () => {
      const [meterList, live] = await Promise.all([
        api.get<Meter[]>(`/api/meters?propertyId=${activeId}`),
        api.get<MeterStatus[]>(`/api/properties/${activeId}/live`),
      ]);
      if (cancelled) return;
      setMeters(meterList);
      setStatuses(Object.fromEntries(live.map((s) => [s.meterId, fromStatus(s)])));
      setSelectedMeterId(meterList[0]?.id ?? null);
    })().catch((e) => setError((e as Error).message));
    return () => { cancelled = true; };
  }, [activeId]);

  // Load recent telemetry for the selected meter.
  useEffect(() => {
    if (!selectedMeterId) { setPoints([]); return; }
    let cancelled = false;
    api.get<TelemetryPoint[]>(`/api/meters/${selectedMeterId}/telemetry?take=500`)
      .then((data) => { if (!cancelled) setPoints(data); })
      .catch((e) => setError((e as Error).message));
    return () => { cancelled = true; };
  }, [selectedMeterId]);

  // Live updates over SignalR for the active property.
  const handleUpdate = useCallback((u: MeterLiveUpdate) => {
    setStatuses((prev) => ({ ...prev, [u.meterId]: fromUpdate(u) }));
    setMeters((prev) => prev.map((m) => (m.id === u.meterId ? { ...m, pairingStatus: 1 } : m)));
    setSelectedMeterId((sel) => {
      if (sel === u.meterId) {
        setPoints((prev) => [
          { observationTime: u.observationTime, totalEnergyKwh: u.totalEnergyKwh, currentLoadKw: u.currentLoadKw, voltage: u.voltage, tariff: u.tariff },
          ...prev,
        ].slice(0, 500));
      }
      return sel;
    });
  }, []);

  useTelemetryHub(activeId, handleUpdate);

  return (
    <div className="app">
      <header className="topbar">
        <strong>Smart Metering</strong>
        <div className="spacer" />
        <span className="muted">{user?.fullName}</span>
        <button onClick={logout}>Одјава</button>
      </header>

      {error && <div className="error banner">{error}</div>}

      <PropertyBar
        properties={properties}
        activeId={activeId}
        onSelect={setActiveId}
        onChanged={loadProperties}
      />

      {activeId ? (
        <main className="content">
          <MeterPanel
            propertyId={activeId}
            meters={meters}
            statuses={statuses}
            selectedMeterId={selectedMeterId}
            onSelectMeter={setSelectedMeterId}
            onChanged={async () => {
              const list = await api.get<Meter[]>(`/api/meters?propertyId=${activeId}`);
              setMeters(list);
              setSelectedMeterId((cur) => (cur && list.some((m) => m.id === cur) ? cur : list[0]?.id ?? null));
            }}
          />
          <section className="charts">
            <VoltageLoadChart points={points} />
            <TariffBarChart points={points} />
          </section>
          {user?.role === 'Consumer' && <LimitPanel />}
          {user?.role === 'Consumer' && <ManualReadingPanel meters={meters} />}
          <InvoicePanel propertyId={activeId} meterId={selectedMeterId} />
        </main>
      ) : (
        <p className="muted pad">Додајте објекат да бисте почели.</p>
      )}
    </div>
  );
}

function PropertyBar({
  properties, activeId, onSelect, onChanged,
}: {
  properties: Property[];
  activeId: string | null;
  onSelect: (id: string) => void;
  onChanged: () => Promise<void>;
}) {
  const emptyForm = { name: '', city: '', address: '', description: '' };
  const [adding, setAdding] = useState(false);
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState(emptyForm);
  const [editForm, setEditForm] = useState(emptyForm);
  const [error, setError] = useState<string | null>(null);
  const active = properties.find((p) => p.id === activeId) ?? null;

  async function add(e: FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      await api.post('/api/properties', form);
      setForm(emptyForm);
      setAdding(false);
      await onChanged();
    } catch (err) {
      setError((err as Error).message);
    }
  }

  function beginEdit() {
    if (!active) return;
    setEditForm({
      name: active.name,
      city: active.city,
      address: active.address,
      description: active.description ?? '',
    });
    setAdding(false);
    setEditing(true);
    setError(null);
  }

  async function update(e: FormEvent) {
    e.preventDefault();
    if (!active) return;
    setError(null);
    try {
      await api.put(`/api/properties/${active.id}`, editForm);
      setEditing(false);
      await onChanged();
    } catch (err) {
      setError((err as Error).message);
    }
  }

  async function remove() {
    if (!active || !window.confirm(`Обрисати објекат „${active.name}”?`)) return;
    setError(null);
    try {
      await api.del(`/api/properties/${active.id}`);
      setEditing(false);
      await onChanged();
    } catch (err) {
      setError((err as Error).message);
    }
  }

  return (
    <div className="property-bar">
      <div className="tabs">
        {properties.map((p) => (
          <button key={p.id} className={p.id === activeId ? 'tab active' : 'tab'} onClick={() => onSelect(p.id)}>
            {p.name}
          </button>
        ))}
        <button className="tab add" onClick={() => { setAdding((a) => !a); setEditing(false); }}>＋ објекат</button>
        {active && <button className="link" onClick={beginEdit}>Измени објекат</button>}
        {active && <button className="link danger" onClick={() => void remove()}>Обриши објекат</button>}
      </div>

      {adding && (
        <form className="inline-form" onSubmit={add}>
          <input placeholder="Назив" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
          <input placeholder="Град" value={form.city} onChange={(e) => setForm({ ...form, city: e.target.value })} required />
          <input placeholder="Адреса" value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} required />
          <input placeholder="Опис (опционо)" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
          <button type="submit">Сачувај</button>
          <button type="button" className="link" onClick={() => setAdding(false)}>Откажи</button>
        </form>
      )}

      {editing && active && (
        <form className="inline-form" onSubmit={update}>
          <input placeholder="Назив" value={editForm.name} onChange={(e) => setEditForm({ ...editForm, name: e.target.value })} required />
          <input placeholder="Град" value={editForm.city} onChange={(e) => setEditForm({ ...editForm, city: e.target.value })} required />
          <input placeholder="Адреса" value={editForm.address} onChange={(e) => setEditForm({ ...editForm, address: e.target.value })} required />
          <input placeholder="Опис (опционо)" value={editForm.description} onChange={(e) => setEditForm({ ...editForm, description: e.target.value })} />
          <button type="submit">Сачувај измене</button>
          <button type="button" className="link" onClick={() => setEditing(false)}>Откажи</button>
        </form>
      )}
      {error && <div className="error property-error">{error}</div>}
    </div>
  );
}

function MeterPanel({
  propertyId, meters, statuses, selectedMeterId, onSelectMeter, onChanged,
}: {
  propertyId: string;
  meters: Meter[];
  statuses: Record<string, LiveView>;
  selectedMeterId: string | null;
  onSelectMeter: (id: string) => void;
  onChanged: () => Promise<void>;
}) {
  const [adding, setAdding] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState({ serialNumber: '', connectionType: 0, note: '' });
  const [editForm, setEditForm] = useState({ connectionType: 0, note: '' });
  const [err, setErr] = useState<string | null>(null);

  async function add(e: FormEvent) {
    e.preventDefault();
    setErr(null);
    try {
      await api.post('/api/meters', { propertyId, ...form, connectionType: Number(form.connectionType) });
      setForm({ serialNumber: '', connectionType: 0, note: '' });
      setAdding(false);
      await onChanged();
    } catch (error) {
      setErr((error as Error).message);
    }
  }

  function beginEdit(meter: Meter) {
    setEditingId(meter.id);
    setEditForm({ connectionType: meter.connectionType, note: meter.note ?? '' });
    setErr(null);
  }

  async function update(e: FormEvent, id: string) {
    e.preventDefault();
    e.stopPropagation();
    setErr(null);
    try {
      await api.put(`/api/meters/${id}`, { ...editForm, connectionType: Number(editForm.connectionType) });
      setEditingId(null);
      await onChanged();
    } catch (error) {
      setErr((error as Error).message);
    }
  }

  async function remove(id: string, serialNumber: string) {
    if (!window.confirm(`Обрисати бројило ${serialNumber}?`)) return;
    setErr(null);
    try {
      await api.del(`/api/meters/${id}`);
      await onChanged();
    } catch (error) {
      setErr((error as Error).message);
    }
  }

  return (
    <section>
      <div className="row between">
        <h2>Бројила</h2>
        <button onClick={() => setAdding((a) => !a)}>＋ бројило</button>
      </div>

      {adding && (
        <form className="inline-form meter-form" onSubmit={add}>
          <input placeholder="SM-YYYY-XXXXX" value={form.serialNumber} onChange={(e) => setForm({ ...form, serialNumber: e.target.value })} required />
          <select value={form.connectionType} onChange={(e) => setForm({ ...form, connectionType: Number(e.target.value) })}>
            <option value={0}>Монофазно</option>
            <option value={1}>Трофазно</option>
          </select>
          <input placeholder="Напомена" value={form.note} onChange={(e) => setForm({ ...form, note: e.target.value })} />
          <button type="submit">Региструј</button>
          <button type="button" className="link" onClick={() => setAdding(false)}>Откажи</button>
        </form>
      )}
      {err && <div className="error">{err}</div>}

      <div className="cards">
        {meters.map((m) => {
          const s = statuses[m.id];
          const selected = m.id === selectedMeterId;
          return (
            <div key={m.id} className={selected ? 'card meter selected' : 'card meter'} onClick={() => onSelectMeter(m.id)}>
              <div className="row between">
                <strong>{m.serialNumber}</strong>
                <span className={`dot ${s?.online ? 'on' : 'off'}`} title={s?.online ? 'online' : 'offline'} />
              </div>
              <div className="muted small">{m.connectionType === 0 ? 'Монофазно' : 'Трофазно'} · {m.maxApprovedPowerKw} kW · {m.pairingStatus === 1 ? 'упарено' : 'неупарено'}</div>
              {m.note && <div className="muted small">{m.note}</div>}
              {s ? (
                <ul className="metrics">
                  <li>Оптерећење <b>{s.load.toFixed(2)} kW</b></li>
                  <li>Напон <b>{s.voltage != null ? `${s.voltage.toFixed(1)} V` : '—'}</b></li>
                  <li>Тарифа <b>{TariffLabel(s.tariff)}</b></li>
                  <li>Укупно <b>{s.totalEnergy.toFixed(2)} kWh</b></li>
                </ul>
              ) : (
                <div className="muted small">Нема још података.</div>
              )}

              {editingId === m.id ? (
                <form className="meter-edit-form" onSubmit={(e) => update(e, m.id)} onClick={(e) => e.stopPropagation()}>
                  <select value={editForm.connectionType} onChange={(e) => setEditForm({ ...editForm, connectionType: Number(e.target.value) })}>
                    <option value={0}>Монофазно</option>
                    <option value={1}>Трофазно</option>
                  </select>
                  <input placeholder="Напомена" value={editForm.note} onChange={(e) => setEditForm({ ...editForm, note: e.target.value })} />
                  <button type="submit">Сачувај</button>
                  <button type="button" className="link" onClick={() => setEditingId(null)}>Откажи</button>
                </form>
              ) : (
                <div className="meter-actions">
                  <button type="button" className="link" onClick={(e) => { e.stopPropagation(); beginEdit(m); }}>Измени</button>
                  <button type="button" className="link danger" onClick={(e) => { e.stopPropagation(); void remove(m.id, m.serialNumber); }}>Обриши</button>
                </div>
              )}
            </div>
          );
        })}
        {meters.length === 0 && <div className="muted">Нема регистрованих бројила.</div>}
      </div>
    </section>
  );
}
