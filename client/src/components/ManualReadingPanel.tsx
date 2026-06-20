import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import { AuthImage } from './AuthImage';
import type { Meter, ManualReadingDto } from '../types';
import { ManualReadingStatusLabel } from '../types';

const statusBadgeClass = (s: ManualReadingDto['status']) =>
  s === 'Processed' ? 'badge paid' : s === 'Rejected' ? 'badge danger' : 'badge';

export function ManualReadingPanel({ meters }: { meters: Meter[] }) {
  const [readings, setReadings] = useState<ManualReadingDto[]>([]);
  const [adding, setAdding] = useState(false);
  const [meterId, setMeterId] = useState('');
  const [declared, setDeclared] = useState('');
  const [note, setNote] = useState('');
  const [image, setImage] = useState<File | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    const data = await api.get<ManualReadingDto[]>('/api/manual-readings/mine');
    setReadings(data);
  }, []);

  useEffect(() => {
    load().catch((e) => setError((e as Error).message));
  }, [load]);

  useEffect(() => {
    setMeterId((cur) => cur || meters[0]?.id || '');
  }, [meters]);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    if (!image) {
      setError('Слика дисплеја бројила је обавезна.');
      return;
    }
    const value = parseFloat(declared);
    if (isNaN(value) || value < 0) {
      setError('Унесите исправно очитано стање (kWh).');
      return;
    }

    setSubmitting(true);
    try {
      const form = new FormData();
      form.append('MeterId', meterId);
      form.append('DeclaredTotalEnergyKwh', String(value));
      if (note) form.append('Note', note);
      form.append('Image', image);

      await api.postForm('/api/manual-readings', form);

      setDeclared('');
      setNote('');
      setImage(null);
      setAdding(false);
      await load();
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setSubmitting(false);
    }
  }

  if (meters.length === 0) return null;

  return (
    <section className="card manual-reading-panel">
      <div className="row between">
        <h3>Пријава стања бројила</h3>
        <button className="link" onClick={() => setAdding((a) => !a)}>
          {adding ? 'Откажи' : '＋ ручно очитавање'}
        </button>
      </div>
      <p className="muted small">
        У случају квара бројила или прекида везе, унесите очитано стање и приложите слику дисплеја.
        Унос остаје на чекању док га администратор наплате не одобри.
      </p>

      {adding && (
        <form className="inline-form manual-reading-form" onSubmit={submit}>
          <select value={meterId} onChange={(e) => setMeterId(e.target.value)} required>
            {meters.map((m) => (
              <option key={m.id} value={m.id}>{m.serialNumber}</option>
            ))}
          </select>
          <input
            type="number" min="0" step="any" placeholder="Очитано стање (kWh)"
            value={declared} onChange={(e) => setDeclared(e.target.value)} required
          />
          <input placeholder="Напомена (опционо)" value={note} onChange={(e) => setNote(e.target.value)} />
          <input
            type="file" accept="image/jpeg,image/png,image/webp"
            onChange={(e) => setImage(e.target.files?.[0] ?? null)} required
          />
          <button type="submit" disabled={submitting}>{submitting ? 'Слање…' : 'Пошаљи'}</button>
        </form>
      )}
      {error && <div className="error">{error}</div>}

      <div className="manual-reading-list">
        {readings.map((r) => (
          <div className="manual-reading-row" key={r.id}>
            <AuthImage path={r.optimizedImageUrl ?? r.originalImageUrl} alt="Слика бројила" className="manual-reading-thumb" />
            <div>
              <strong>{r.serialNumber}</strong> · {r.declaredTotalEnergyKwh.toLocaleString('sr-Latn')} kWh
              <div className="muted small">{new Date(r.submittedAtUtc).toLocaleString('sr-RS')}</div>
              {r.note && <div className="muted small">„{r.note}”</div>}
              {r.reviewNote && <div className="muted small">Напомена администратора: „{r.reviewNote}”</div>}
            </div>
            <span className={statusBadgeClass(r.status)}>{ManualReadingStatusLabel(r.status)}</span>
          </div>
        ))}
        {readings.length === 0 && <p className="muted small">Још нема ручних очитавања.</p>}
      </div>
    </section>
  );
}
