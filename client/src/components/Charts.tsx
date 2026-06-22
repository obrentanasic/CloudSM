import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import type { TelemetryPoint } from '../types';

const fmtTime = (iso: string) => new Date(iso).toLocaleTimeString('sr-RS', { hour: '2-digit', minute: '2-digit', second: '2-digit' });

/** Voltage + load trend over the recent window (oldest -> newest). */
export function VoltageLoadChart({ points }: { points: TelemetryPoint[] }) {
  const data = [...points]
    .sort((a, b) => +new Date(a.observationTime) - +new Date(b.observationTime))
    .map((p) => ({
      time: fmtTime(p.observationTime),
      voltage: p.voltage ?? null,
      load: p.currentLoadKw,
    }));

  return (
    <div className="chart">
      <h3>Напон и оптерећење</h3>
      <ResponsiveContainer width="100%" height={240}>
        <LineChart data={data}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="time" minTickGap={24} />
          <YAxis yAxisId="v" domain={[160, 250]} width={40} />
          <YAxis yAxisId="l" orientation="right" width={40} />
          <Tooltip />
          <Legend />
          <Line yAxisId="v" type="monotone" dataKey="voltage" name="Напон (V)" stroke="#2563eb" dot={false} />
          <Line yAxisId="l" type="monotone" dataKey="load" name="Оптерећење (kW)" stroke="#16a34a" dot={false} />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}

export interface DailyTariffPoint {
  day: string;
  vt: number;
  nt: number;
}

export function buildDailyTariffData(points: TelemetryPoint[]): DailyTariffPoint[] {
  const sorted = [...points].sort((a, b) => +new Date(a.observationTime) - +new Date(b.observationTime));
  const totals = new Map<string, DailyTariffPoint>();

  for (let i = 1; i < sorted.length; i++) {
    const delta = sorted[i].totalEnergyKwh - sorted[i - 1].totalEnergyKwh;
    if (delta <= 0) continue;

    const date = new Date(sorted[i].observationTime);
    const key = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
    const item = totals.get(key) ?? {
      day: date.toLocaleDateString('sr-RS', { day: '2-digit', month: '2-digit' }),
      vt: 0,
      nt: 0,
    };

    if (sorted[i].tariff === 0) item.vt += delta;
    else item.nt += delta;
    totals.set(key, item);
  }

  return [...totals.values()].map((item) => ({
    ...item,
    vt: Number(item.vt.toFixed(3)),
    nt: Number(item.nt.toFixed(3)),
  }));
}

/** Daily energy consumption split into high and low tariff columns. */
export function TariffBarChart({ points }: { points: TelemetryPoint[] }) {
  const data = buildDailyTariffData(points);

  return (
    <div className="chart">
      <h3>Дневна потрошња по тарифи (kWh)</h3>
      <ResponsiveContainer width="100%" height={240}>
        <BarChart data={data}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="day" />
          <YAxis width={40} />
          <Tooltip />
          <Legend />
          <Bar dataKey="vt" name="ВТ" fill="#2563eb" />
          <Bar dataKey="nt" name="НТ" fill="#f59e0b" />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}
