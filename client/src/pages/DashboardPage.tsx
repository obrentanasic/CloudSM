import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { useTelemetryHub } from '../hooks/useTelemetryHub';
import { TariffBarChart, VoltageLoadChart } from '../components/Charts';
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
    setActiveId((cur) => cur ?? data[0]?.id ?? null);
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
    api.get<TelemetryPoint[]>(`/api/meters/${selectedMeterId}/telemetry?take=100`)
      .then((data) => { if (!cancelled) setPoints(data); })
      .catch((e) => setError((e as Error).message));
    return () => { cancelled = true; };
  }, [selectedMeterId]);

  // Live updates over SignalR for the active property.
  const handleUpdate = useCallback((u: MeterLiveUpdate) => {
    setStatuses((prev) => ({ ...prev, [u.meterId]: fromUpdate(u) }));
    setSelectedMeterId((sel) => {
      if (sel === u.meterId) {
        setPoints((prev) => [
          { observationTime: u.observationTime, totalEnergyKwh: u.totalEnergyKwh, currentLoadKw: u.currentLoadKw, voltage: u.voltage, tariff: u.tariff },
          ...prev,
        ].slice(0, 100));
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
            }}
          />
          <section className="charts">
            <VoltageLoadChart points={points} />
            <TariffBarChart points={points} />
          </section>
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
  const [adding, setAdding] = useState(false);
  const [form, setForm] = useState({ name: '', city: '', address: '', description: '' });

  async function add(e: FormEvent) {
    e.preventDefault();
    await api.post('/api/properties', form);
    setForm({ name: '', city: '', address: '', description: '' });
    setAdding(false);
    await onChanged();
  }

  return (
    <div className="tabs">
      {properties.map((p) => (
        <button key={p.id} className={p.id === activeId ? 'tab active' : 'tab'} onClick={() => onSelect(p.id)}>
          {p.name}
        </button>
      ))}
      <button className="tab add" onClick={() => setAdding((a) => !a)}>＋ објекат</button>
      {adding && (
        <form className="inline-form" onSubmit={add}>
          <input placeholder="Назив" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
          <input placeholder="Град" value={form.city} onChange={(e) => setForm({ ...form, city: e.target.value })} required />
          <input placeholder="Адреса" value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} required />
          <button type="submit">Сачувај</button>
        </form>
      )}
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
  const [form, setForm] = useState({ serialNumber: '', connectionType: 0, note: '' });
  const [err, setErr] = useState<string | null>(null);

  async function add(e: FormEvent) {
    e.preventDefault();
    setErr(null);
    try {
      await api.post('/api/meters', { propertyId, ...form, connectionType: Number(form.connectionType) });
      setForm({ serialNumber: '', connectionType: 0, note: '' });
      setAdding(false);
      await onChanged();
    } catch (e2) {
      setErr((e2 as Error).message);
    }
  }

  async function remove(id: string) {
    await api.del(`/api/meters/${id}`);
    await onChanged();
  }

  return (
    <section>
      <div className="row between">
        <h2>Бројила</h2>
        <button onClick={() => setAdding((a) => !a)}>＋ бројило</button>
      </div>

      {adding && (
        <form className="inline-form" onSubmit={add}>
          <input placeholder="SM-YYYY-XXXXX" value={form.serialNumber} onChange={(e) => setForm({ ...form, serialNumber: e.target.value })} required />
          <select value={form.connectionType} onChange={(e) => setForm({ ...form, connectionType: Number(e.target.value) })}>
            <option value={0}>Монофазно</option>
            <option value={1}>Трофазно</option>
          </select>
          <input placeholder="Напомена" value={form.note} onChange={(e) => setForm({ ...form, note: e.target.value })} />
          <button type="submit">Региструј</button>
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
              <button className="link danger" onClick={(e) => { e.stopPropagation(); remove(m.id); }}>Обриши</button>
            </div>
          );
        })}
        {meters.length === 0 && <div className="muted">Нема регистрованих бројила.</div>}
      </div>
    </section>
  );
}
