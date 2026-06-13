import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import type { LimitDto } from '../types';
import { LimitUnit, LimitUnitLabel } from '../types';

export function LimitPanel() {
  const [current, setCurrent] = useState<LimitDto | null>(null);
  const [loaded, setLoaded] = useState(false);
  const [editing, setEditing] = useState(false);
  const [value, setValue] = useState('');
  const [unit, setUnit] = useState<number>(LimitUnit.Kwh);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api
      .get<LimitDto | null>('/api/limit')
      .then((data) => {
        setCurrent(data);
        if (data) {
          setValue(String(data.value));
          setUnit(data.unit);
        }
      })
      .catch(() => {
        /* 204 → null, or network error — just show "no limit" */
      })
      .finally(() => setLoaded(true));
  }, []);

  async function save(e: FormEvent) {
    e.preventDefault();
    const parsed = parseFloat(value);
    if (isNaN(parsed) || parsed <= 0) {
      setError('Унесите позитиван број.');
      return;
    }
    setSaving(true);
    setError(null);
    try {
      await api.put('/api/limit', { value: parsed, unit });
      setCurrent({ value: parsed, unit });
      setEditing(false);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setSaving(false);
    }
  }

  if (!loaded) return null;

  return (
    <section className="limit-panel card">
      <div className="row between">
        <h3 className="limit-title">
          <span className="limit-icon">⚡</span>
          Лимит потрошње
        </h3>
        {!editing && (
          <button className="link" onClick={() => setEditing(true)}>
            {current ? 'Измени' : 'Постави'}
          </button>
        )}
      </div>

      {!editing && current && (
        <div className="limit-display">
          <span className="limit-value">{current.value.toLocaleString('sr-Latn')}</span>
          <span className="limit-unit">{LimitUnitLabel(current.unit)}</span>
          <span className="muted small" style={{ marginLeft: 8 }}>месечно</span>
        </div>
      )}

      {!editing && !current && (
        <p className="muted small">
          Нисте поставили лимит. Поставите га да бисте добијали обавештења
          кад потрошња пређе задату границу.
        </p>
      )}

      {editing && (
        <form className="limit-form" onSubmit={save}>
          <label>
            Вредност
            <input
              id="limit-value"
              type="number"
              min="0.001"
              step="any"
              value={value}
              onChange={(e) => setValue(e.target.value)}
              placeholder="нпр. 350"
              required
              autoFocus
            />
          </label>
          <label>
            Јединица
            <select
              id="limit-unit"
              value={unit}
              onChange={(e) => setUnit(Number(e.target.value))}
            >
              <option value={LimitUnit.Kwh}>kWh</option>
              <option value={LimitUnit.Rsd}>РСД (динари)</option>
            </select>
          </label>
          <div className="limit-actions">
            <button type="submit" disabled={saving}>
              {saving ? 'Чување…' : 'Сачувај'}
            </button>
            <button
              type="button"
              className="link"
              onClick={() => {
                setEditing(false);
                if (current) {
                  setValue(String(current.value));
                  setUnit(current.unit);
                }
              }}
            >
              Откажи
            </button>
          </div>
          {error && <div className="error">{error}</div>}
        </form>
      )}
    </section>
  );
}
