import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
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

/** Voltage + load trend over the recent window (oldest → newest). */
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

/** Energy consumed per tariff over the recent window, derived from cumulative-reading deltas. */
export function TariffBarChart({ points }: { points: TelemetryPoint[] }) {
  const sorted = [...points].sort((a, b) => +new Date(a.observationTime) - +new Date(b.observationTime));
  let vt = 0;
  let nt = 0;
  for (let i = 1; i < sorted.length; i++) {
    const delta = sorted[i].totalEnergyKwh - sorted[i - 1].totalEnergyKwh;
    if (delta <= 0) continue;
    if (sorted[i].tariff === 0) vt += delta;
    else nt += delta;
  }

  const data = [
    { name: 'ВТ', kWh: Number(vt.toFixed(3)) },
    { name: 'НТ', kWh: Number(nt.toFixed(3)) },
  ];

  return (
    <div className="chart">
      <h3>Потрошња по тарифи (kWh)</h3>
      <ResponsiveContainer width="100%" height={240}>
        <BarChart data={data}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="name" />
          <YAxis width={40} />
          <Tooltip />
          <Bar dataKey="kWh" name="kWh">
            <Cell fill="#2563eb" />
            <Cell fill="#f59e0b" />
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}
