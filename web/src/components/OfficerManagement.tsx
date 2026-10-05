import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import { api, type AdminUser } from '../utils/marketApi';
import { ordersApi, type CollectionCentreResponse } from '../utils/ordersApi';
import { useEscapeKey } from '../hooks/useEscapeKey';
import { readableError } from '../utils/apiErrors';
import { Toast, type ToastData } from './Toast';

/** 12 characters with letters and digits, from the browser's secure random source. */
function generatePassword(): string {
  const letters = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz';
  const digits = '23456789';
  const all = letters + digits;
  const bytes = new Uint32Array(12);
  crypto.getRandomValues(bytes);
  const chars = Array.from(bytes, (b) => all[b % all.length]);
  chars[0] = letters[bytes[0] % letters.length];
  chars[1] = digits[bytes[1] % digits.length];
  return chars.join('');
}

function passwordProblem(password: string): string | null {
  if (password.length < 8) return 'The password must be at least 8 characters.';
  if (!/[A-Za-z]/.test(password) || !/\d/.test(password)) return 'The password needs a letter and a digit.';
  return null;
}

interface Issued {
  title: string;
  email: string;
  password: string;
}

/**
 * Administrator -> Officers: list, add (bound to a collection centre), activate / deactivate,
 * reset password. An Officer without a centre would see every centre's orders, so the form
 * requires one.
 */
export function OfficerManagement() {
  const [officers, setOfficers] = useState<AdminUser[]>([]);
  const [centres, setCentres] = useState<CollectionCentreResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [toast, setToast] = useState<ToastData | null>(null);
  const [showAdd, setShowAdd] = useState(false);
  const [resetTarget, setResetTarget] = useState<AdminUser | null>(null);
  const [issued, setIssued] = useState<Issued | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const centreName = useMemo(() => new Map(centres.map((c) => [c.id, c.name])), [centres]);

  const notify = (message: string, error = false) => setToast({ kind: error ? 'error' : 'success', message });

  const load = useCallback(async () => {
    setLoading(true);
    setLoadError(null);
    try {
      const [page, centreList] = await Promise.all([
        api.listUsers({ role: 'Officer', size: 100 }),
        ordersApi.listCentres(),
      ]);
      setOfficers(page.items);
      setCentres(centreList);
    } catch (err) {
      setLoadError(readableError(err, 'Could not load the officers.'));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const visible = officers.filter((o) => {
    const q = search.trim().toLowerCase();
    return !q || o.fullName.toLowerCase().includes(q) || o.email.toLowerCase().includes(q);
  });

  const toggleActive = async (officer: AdminUser) => {
    const next = !officer.isActive;
    if (!next && !window.confirm(`Deactivate ${officer.fullName}? They will no longer be able to sign in.`)) return;
    setBusyId(officer.id);
    try {
      const updated = await api.setUserActive(officer.id, next);
      setOfficers((all) => all.map((o) => (o.id === updated.id ? updated : o)));
      notify(`${officer.fullName} ${next ? 'activated' : 'deactivated'}.`);
    } catch (err) {
      notify(readableError(err, 'Could not change the account status.'), true);
    } finally {
      setBusyId(null);
    }
  };

  return (
    <div>
      <Toast toast={toast} onClose={() => setToast(null)} />

      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '12px', flexWrap: 'wrap', marginBottom: '16px' }}>
        <div>
          <h2 style={{ fontSize: '1.2rem', fontWeight: 700, color: 'var(--text)', margin: 0 }}>Collection-centre officers ({officers.length})</h2>
          <p style={{ margin: '4px 0 0', color: 'var(--text-muted)', fontSize: '0.85rem' }}>
            Officers inspect produce, approve orders and schedule pickups for their own collection centre.
          </p>
        </div>
        <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
          <input
            className="form-input"
            aria-label="Search officers"
            placeholder="Search name or email"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            style={{ minWidth: '220px' }}
          />
          <button className="btn btn-primary" onClick={() => setShowAdd(true)} disabled={centres.length === 0}>
            + Add officer
          </button>
        </div>
      </div>

      {loading && <p style={{ color: 'var(--text-muted)' }}>Loading officers…</p>}

      {loadError && (
        <div role="alert" style={{ padding: '14px', border: '1px solid var(--danger-border)', background: 'var(--danger-soft)', color: 'var(--danger)', borderRadius: '12px', display: 'flex', gap: '12px', alignItems: 'center', justifyContent: 'space-between' }}>
          <span>{loadError}</span>
          <button className="btn btn-secondary" onClick={load}>Try again</button>
        </div>
      )}

      {!loading && !loadError && visible.length === 0 && (
        <div style={{ textAlign: 'center', padding: '40px 16px', border: '1px dashed var(--border-strong)', borderRadius: '12px', color: 'var(--text-muted)' }}>
          {officers.length === 0 ? 'No officers yet. Add the first one.' : 'No officers match your search.'}
        </div>
      )}

      {!loading && !loadError && visible.length > 0 && (
        <div className="glass-card" style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', minWidth: '760px' }}>
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Collection centre</th>
                <th>Status</th>
                <th>Added</th>
                <th style={{ textAlign: 'right' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {visible.map((o) => (
                <tr key={o.id}>
                  <td>
                    <strong>{o.fullName}</strong>
                    {o.phone && <div style={{ fontSize: '0.78rem', color: 'var(--text-muted)' }}>{o.phone}</div>}
                  </td>
                  <td>{o.email}</td>
                  <td>{(o.collectionCentreId && centreName.get(o.collectionCentreId)) || <span style={{ color: 'var(--danger)' }}>No centre assigned</span>}</td>
                  <td>
                    <span className={`badge ${o.isActive ? 'badge-published' : 'badge-withdrawn'}`}>{o.isActive ? 'Active' : 'Deactivated'}</span>
                  </td>
                  <td>{new Date(o.createdAt).toLocaleDateString()}</td>
                  <td style={{ textAlign: 'right' }}>
                    <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end', flexWrap: 'wrap' }}>
                      <button className="btn btn-secondary" style={{ padding: '6px 12px', fontSize: '0.78rem' }} onClick={() => setResetTarget(o)}>
                        Reset password
                      </button>
                      <button
                        className={o.isActive ? 'btn btn-danger' : 'btn btn-primary'}
                        style={{ padding: '6px 12px', fontSize: '0.78rem' }}
                        disabled={busyId === o.id}
                        onClick={() => toggleActive(o)}
                      >
                        {o.isActive ? 'Deactivate' : 'Activate'}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showAdd && (
        <AddOfficerModal
          centres={centres}
          onClose={() => setShowAdd(false)}
          onCreated={(officer, password) => {
            setShowAdd(false);
            setOfficers((all) => [officer, ...all]);
            setIssued({ title: 'Officer added', email: officer.email, password });
          }}
        />
      )}

      {resetTarget && (
        <ResetPasswordModal
          officer={resetTarget}
          onClose={() => setResetTarget(null)}
          onDone={(password) => {
            setIssued({ title: 'Password reset', email: resetTarget.email, password });
            setResetTarget(null);
          }}
        />
      )}

      {issued && <CredentialsModal issued={issued} onClose={() => setIssued(null)} />}
    </div>
  );
}

function PasswordField({ value, onChange, label = 'Temporary password' }: { value: string; onChange: (v: string) => void; label?: string }) {
  const [visible, setVisible] = useState(false);
  return (
    <div className="form-group">
      <label className="form-label" htmlFor="officer-password">{label} *</label>
      <div style={{ display: 'flex', gap: '8px' }}>
        <input
          id="officer-password"
          className="form-input"
          style={{ flex: 1 }}
          type={visible ? 'text' : 'password'}
          autoComplete="new-password"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          required
        />
        <button type="button" className="btn btn-secondary" onClick={() => setVisible((v) => !v)}>{visible ? 'Hide' : 'Show'}</button>
        <button type="button" className="btn btn-secondary" onClick={() => { onChange(generatePassword()); setVisible(true); }}>Generate</button>
      </div>
      <span style={{ fontSize: '0.75rem', color: 'var(--text-faint)' }}>At least 8 characters, with a letter and a digit.</span>
    </div>
  );
}

function AddOfficerModal({ centres, onClose, onCreated }: {
  centres: CollectionCentreResponse[];
  onClose: () => void;
  onCreated: (officer: AdminUser, password: string) => void;
}) {
  useEscapeKey(onClose);
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [centreId, setCentreId] = useState(centres[0]?.id ?? '');
  const [password, setPassword] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!fullName.trim()) return setError('Enter the officer’s full name.');
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) return setError('Enter a valid email address.');
    if (!centreId) return setError('Choose the collection centre this officer works at.');
    const problem = passwordProblem(password);
    if (problem) return setError(problem);

    setSaving(true);
    try {
      const officer = await api.createManagedUser({
        fullName: fullName.trim(),
        email: email.trim(),
        password,
        role: 'Officer',
        phone: phone.trim() || undefined,
        collectionCentreId: centreId,
      });
      onCreated(officer, password);
    } catch (err) {
      setError(readableError(err, 'Could not add the officer.'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={saving ? undefined : onClose}>
      <form className="modal-content" style={{ maxWidth: '520px' }} onClick={(e) => e.stopPropagation()} onSubmit={submit} noValidate>
        <div style={{ padding: '18px 22px', borderBottom: '1px solid var(--border)' }}>
          <h3 style={{ margin: 0, fontSize: '1.05rem', color: 'var(--text)' }}>Add collection-centre officer</h3>
          <p style={{ margin: '4px 0 0', fontSize: '0.8rem', color: 'var(--text-muted)' }}>They will only see orders and schedules for the centre you choose.</p>
        </div>
        <div style={{ padding: '18px 22px' }}>
          <div className="form-group">
            <label className="form-label" htmlFor="officer-name">Full name *</label>
            <input id="officer-name" className="form-input" value={fullName} onChange={(e) => setFullName(e.target.value)} maxLength={100} autoFocus />
          </div>
          <div className="form-group">
            <label className="form-label" htmlFor="officer-email">Email *</label>
            <input id="officer-email" className="form-input" type="email" value={email} onChange={(e) => setEmail(e.target.value)} maxLength={150} autoComplete="off" />
          </div>
          <div className="form-group">
            <label className="form-label" htmlFor="officer-phone">Phone</label>
            <input id="officer-phone" className="form-input" type="tel" value={phone} onChange={(e) => setPhone(e.target.value)} maxLength={20} placeholder="+94 7X XXX XXXX" />
          </div>
          <div className="form-group">
            <label className="form-label" htmlFor="officer-centre">Collection centre *</label>
            <select id="officer-centre" className="form-input" value={centreId} onChange={(e) => setCentreId(e.target.value)}>
              {centres.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>
          <PasswordField value={password} onChange={setPassword} />
          {error && (
            <div role="alert" style={{ color: 'var(--danger)', background: 'var(--danger-soft)', border: '1px solid var(--danger-border)', borderRadius: '10px', padding: '10px 12px', fontSize: '0.85rem' }}>
              {error}
            </div>
          )}
        </div>
        <div style={{ padding: '14px 22px', borderTop: '1px solid var(--border)', display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
          <button type="button" className="btn btn-secondary" onClick={onClose} disabled={saving}>Cancel</button>
          <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Adding…' : 'Add officer'}</button>
        </div>
      </form>
    </div>
  );
}

function ResetPasswordModal({ officer, onClose, onDone }: { officer: AdminUser; onClose: () => void; onDone: (password: string) => void }) {
  useEscapeKey(onClose);
  const [password, setPassword] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    const problem = passwordProblem(password);
    if (problem) return setError(problem);
    setSaving(true);
    setError(null);
    try {
      await api.resetUserPassword(officer.id, password);
      onDone(password);
    } catch (err) {
      setError(readableError(err, 'Could not reset the password.'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={saving ? undefined : onClose}>
      <form className="modal-content" style={{ maxWidth: '480px' }} onClick={(e) => e.stopPropagation()} onSubmit={submit} noValidate>
        <div style={{ padding: '18px 22px', borderBottom: '1px solid var(--border)' }}>
          <h3 style={{ margin: 0, fontSize: '1.05rem', color: 'var(--text)' }}>Reset password</h3>
          <p style={{ margin: '4px 0 0', fontSize: '0.8rem', color: 'var(--text-muted)' }}>{officer.fullName} · {officer.email}</p>
        </div>
        <div style={{ padding: '18px 22px' }}>
          <PasswordField value={password} onChange={setPassword} label="New temporary password" />
          {error && (
            <div role="alert" style={{ color: 'var(--danger)', background: 'var(--danger-soft)', border: '1px solid var(--danger-border)', borderRadius: '10px', padding: '10px 12px', fontSize: '0.85rem' }}>
              {error}
            </div>
          )}
        </div>
        <div style={{ padding: '14px 22px', borderTop: '1px solid var(--border)', display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
          <button type="button" className="btn btn-secondary" onClick={onClose} disabled={saving}>Cancel</button>
          <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Saving…' : 'Reset password'}</button>
        </div>
      </form>
    </div>
  );
}

/** Shown once after a create/reset: the password is not stored anywhere readable. */
function CredentialsModal({ issued, onClose }: { issued: Issued; onClose: () => void }) {
  useEscapeKey(onClose);
  const [copied, setCopied] = useState(false);

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(`Email: ${issued.email}\nTemporary password: ${issued.password}`);
      setCopied(true);
    } catch {
      setCopied(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" style={{ maxWidth: '480px' }} onClick={(e) => e.stopPropagation()}>
        <div style={{ padding: '18px 22px', borderBottom: '1px solid var(--border)' }}>
          <h3 style={{ margin: 0, fontSize: '1.05rem', color: 'var(--text)' }}>{issued.title}</h3>
        </div>
        <div style={{ padding: '18px 22px', display: 'grid', gap: '12px' }}>
          <p style={{ margin: 0, fontSize: '0.88rem', color: 'var(--text-muted)' }}>
            Give these sign-in details to the officer. The password is shown only now and cannot be looked up later — you can reset it again if it is lost.
          </p>
          <div style={{ background: 'var(--bg-hover)', border: '1px solid var(--border)', borderRadius: '10px', padding: '12px 14px', fontFamily: 'var(--font-mono, monospace)', fontSize: '0.9rem', wordBreak: 'break-all' }}>
            <div>Email: {issued.email}</div>
            <div>Temporary password: {issued.password}</div>
          </div>
        </div>
        <div style={{ padding: '14px 22px', borderTop: '1px solid var(--border)', display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
          <button type="button" className="btn btn-secondary" onClick={copy}>{copied ? 'Copied' : 'Copy'}</button>
          <button type="button" className="btn btn-primary" onClick={onClose}>Done</button>
        </div>
      </div>
    </div>
  );
}
