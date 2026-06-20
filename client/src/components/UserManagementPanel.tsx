import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import type { CreateUserRequest, UserAccount } from '../types';
import { UserRoleLabel, UserStatusLabel } from '../types';

const emptyForm: CreateUserRequest = {
  firstName: '',
  lastName: '',
  email: '',
  phoneNumber: '',
  role: 1, // Consumer
};

const statusBadge = (s: UserAccount['status']) =>
  s === 'Active' ? 'badge paid' : s === 'Suspended' ? 'badge unpaid' : 'badge';

export function UserManagementPanel() {
  const [users, setUsers] = useState<UserAccount[]>([]);
  const [form, setForm] = useState<CreateUserRequest>(emptyForm);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const load = useCallback(async () => {
    const data = await api.get<UserAccount[]>('/api/auth/users');
    setUsers(data);
  }, []);

  useEffect(() => {
    load().catch((e) => setError((e as Error).message));
  }, [load]);

  async function createUser(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setNotice(null);
    try {
      await api.post('/api/auth/users', { ...form, role: Number(form.role) });
      setNotice(`Налог за ${form.email} је креиран — послат је активациони мејл.`);
      setForm(emptyForm);
      await load();
    } catch (err) {
      setError((err as Error).message);
    }
  }

  async function act(id: string, action: () => Promise<unknown>) {
    setError(null);
    setNotice(null);
    setBusyId(id);
    try {
      await action();
      await load();
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setBusyId(null);
    }
  }

  const suspend = (u: UserAccount) => act(u.id, () => api.post(`/api/auth/users/${u.id}/suspend`));
  const reactivate = (u: UserAccount) => act(u.id, () => api.post(`/api/auth/users/${u.id}/reactivate`));
  const remove = (u: UserAccount) => {
    if (!window.confirm(`Обрисати налог ${u.email}? Ова акција је неповратна.`)) return;
    void act(u.id, () => api.del(`/api/auth/users/${u.id}`));
  };

  return (
    <main className="content admin-grid">
      {error && <div className="error banner span-full">{error}</div>}
      {notice && <div className="banner success span-full">{notice}</div>}

      <section className="card tariff-card span-full">
        <div className="row between">
          <h2>Корисници</h2>
          <span className="muted small">{users.length} налога</span>
        </div>
        <div className="tariff-list">
          {users.map((u) => (
            <div className="tariff-row" key={u.id}>
              <div>
                <strong>{u.firstName} {u.lastName}</strong>
                <div className="muted small">
                  {u.email} · {UserRoleLabel(u.role)} · од {new Date(u.createdAtUtc).toLocaleDateString('sr-RS')}
                </div>
              </div>
              <span className={statusBadge(u.status)}>{UserStatusLabel(u.status)}</span>
              {u.status === 'Suspended' ? (
                <button className="link" disabled={busyId === u.id} onClick={() => reactivate(u)}>Реактивирај</button>
              ) : (
                <button className="link" disabled={busyId === u.id} onClick={() => suspend(u)}>Суспендуј</button>
              )}
              <button className="link danger" disabled={busyId === u.id} onClick={() => remove(u)}>Обриши</button>
            </div>
          ))}
          {users.length === 0 && <p className="muted small">Нема корисника.</p>}
        </div>
      </section>

      <section className="card tariff-card span-full">
        <h2>Нови кориснички налог</h2>
        <p className="muted small">
          Систем не подржава јавну регистрацију. Администратор уноса податке и додељује улогу;
          кориснику се шаље активациони линк за постављање лозинке.
        </p>
        <form className="tariff-form" onSubmit={createUser}>
          <label>Име<input value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} required /></label>
          <label>Презиме<input value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} required /></label>
          <label>Имејл<input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} required /></label>
          <label>Телефон<input value={form.phoneNumber} onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })} required /></label>
          <label>Улога
            <select value={form.role} onChange={(e) => setForm({ ...form, role: Number(e.target.value) })}>
              <option value={1}>Потрошач</option>
              <option value={2}>Администратор наплате</option>
              <option value={0}>Администратор</option>
            </select>
          </label>
          <button type="submit">Креирај налог</button>
        </form>
      </section>
    </main>
  );
}
