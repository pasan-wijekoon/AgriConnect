import React, { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { Sprout, Lock, Mail } from '../components/Icons';

// All 25 Sri Lankan Districts
const SRI_LANKA_DISTRICTS = [
  'Colombo', 'Gampaha', 'Kalutara',
  'Kandy', 'Matale', 'Nuwara Eliya',
  'Galle', 'Matara', 'Hambantota',
  'Jaffna', 'Kilinochchi', 'Mannar', 'Mullaitivu', 'Vavuniya',
  'Batticaloa', 'Ampara', 'Trincomalee',
  'Kurunegala', 'Puttalam',
  'Anuradhapura', 'Polonnaruwa',
  'Badulla', 'Monaragala',
  'Ratnapura', 'Kegalle'
];

export const LoginPage: React.FC = () => {
  const { login, register } = useAuth();
  const [isRegister, setIsRegister] = useState(false);
  
  // Login form state
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  
  // Register form state
  const [fullName, setFullName] = useState('');
  const [regRole, setRegRole] = useState<'Farmer' | 'Buyer'>('Farmer');
  const [phone, setPhone] = useState('');
  const [region, setRegion] = useState('Colombo');

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  // Phone number validation: must be 10 digits starting with 07
  const validatePhone = (value: string): string | null => {
    const digits = value.replace(/[\s\-()]/g, '');
    if (!digits) return 'Phone number is required';
    if (!/^07\d{8}$/.test(digits)) {
      return 'Phone must be 10 digits starting with 07 (e.g. 077-123 4567)';
    }
    return null;
  };

  // Format phone as user types: 07X-XXX XXXX
  const handlePhoneChange = (value: string) => {
    // Strip non-digits
    let digits = value.replace(/\D/g, '');
    if (digits.length > 10) digits = digits.substring(0, 10);
    
    // Format: 07X-XXX XXXX
    let formatted = digits;
    if (digits.length > 3 && digits.length <= 6) {
      formatted = digits.substring(0, 3) + '-' + digits.substring(3);
    } else if (digits.length > 6) {
      formatted = digits.substring(0, 3) + '-' + digits.substring(3, 6) + ' ' + digits.substring(6);
    }
    setPhone(formatted);
    
    // Clear phone error on edit
    if (fieldErrors.phone) {
      setFieldErrors(prev => ({ ...prev, phone: '' }));
    }
  };

  const handleLoginSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await login(email, password);
    } catch (err: any) {
      setError(err.message || 'Login failed. Please check credentials.');
    } finally {
      setLoading(false);
    }
  };

  const handleRegisterSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    // Client-side validation
    const errors: Record<string, string> = {};
    if (!fullName.trim()) errors.fullName = 'Full name is required';
    if (!email.trim()) errors.email = 'Email is required';
    if (!password || password.length < 6) errors.password = 'Password must be at least 6 characters';
    
    const phoneErr = validatePhone(phone);
    if (phoneErr) errors.phone = phoneErr;
    
    if (!region) errors.region = 'District is required';

    if (Object.keys(errors).length > 0) {
      setFieldErrors(errors);
      return;
    }

    setFieldErrors({});
    setLoading(true);
    try {
      await register({
        fullName: fullName.trim(),
        email: email.trim(),
        password,
        role: regRole,
        phone: phone.replace(/[\s\-]/g, ''), // Send raw digits
        region
      });
    } catch (err: any) {
      setError(err.message || 'Registration failed.');
    } finally {
      setLoading(false);
    }
  };

  const requiredStar = <span style={{ color: 'var(--danger)', marginLeft: '2px' }}>*</span>;

  const fieldErrorStyle: React.CSSProperties = {
    color: '#e08776',
    fontSize: '0.75rem',
    marginTop: '4px',
  };

  return (
    <div style={{
      minHeight: '100vh',
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      padding: '2rem 1rem',
    }}>
      {/* Header Branding */}
      <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
        <div style={{
          display: 'inline-flex',
          alignItems: 'center',
          justifyContent: 'center',
          width: '52px',
          height: '52px',
          borderRadius: 'var(--radius-md)',
          background: 'var(--accent)',
          marginBottom: '1rem',
        }}>
          <Sprout size={28} style={{ color: '#06120c' }} />
        </div>
        <h1 style={{ fontSize: '1.8rem', fontWeight: 700, color: 'var(--text)' }}>
          AgriConnect
        </h1>
        <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem', marginTop: '6px', maxWidth: '400px' }}>
          Produce listings & fair-price discovery platform
        </p>
      </div>

      {/* Auth Card (Sign In / Register) */}
      <div className="glass-card" style={{ width: '100%', maxWidth: '420px', padding: '1.75rem' }}>
        {/* Tab Toggle */}
        <div style={{
          display: 'flex',
          background: 'var(--bg-input)',
          borderRadius: 'var(--radius-sm)',
          padding: '4px',
          marginBottom: '1.5rem',
          border: '1px solid var(--border)'
        }}>
          <button
            onClick={() => { setIsRegister(false); setError(null); setFieldErrors({}); }}
            style={{
              flex: 1,
              padding: '8px',
              borderRadius: '6px',
              border: 'none',
              fontSize: '0.85rem',
              fontWeight: 500,
              cursor: 'pointer',
              background: !isRegister ? 'var(--bg-hover)' : 'transparent',
              color: !isRegister ? 'var(--text)' : 'var(--text-muted)',
            }}
          >
            Sign In
          </button>
          <button
            onClick={() => { setIsRegister(true); setError(null); setFieldErrors({}); }}
            style={{
              flex: 1,
              padding: '8px',
              borderRadius: '6px',
              border: 'none',
              fontSize: '0.85rem',
              fontWeight: 500,
              cursor: 'pointer',
              background: isRegister ? 'var(--bg-hover)' : 'transparent',
              color: isRegister ? 'var(--text)' : 'var(--text-muted)',
            }}
          >
            Register
          </button>
        </div>

        {error && (
          <div style={{
            background: 'var(--danger-soft)',
            border: '1px solid var(--danger-border)',
            color: '#e08776',
            padding: '10px 14px',
            borderRadius: 'var(--radius-sm)',
            fontSize: '0.85rem',
            marginBottom: '1rem'
          }}>
            {error}
          </div>
        )}

        {!isRegister ? (
          /* Sign In Form */
          <form onSubmit={handleLoginSubmit}>
            <div className="form-group">
              <label className="form-label">Email Address</label>
              <div style={{ position: 'relative' }}>
                <input
                  type="email"
                  className="form-input"
                  style={{ width: '100%', paddingLeft: '38px' }}
                  placeholder="name@agriconnect.lk"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                />
                <Mail size={16} style={{ position: 'absolute', left: '12px', top: '13px', color: 'var(--text-faint)' }} />
              </div>
            </div>

            <div className="form-group">
              <label className="form-label">Password</label>
              <div style={{ position: 'relative' }}>
                <input
                  type="password"
                  className="form-input"
                  style={{ width: '100%', paddingLeft: '38px' }}
                  placeholder="••••••••"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                />
                <Lock size={16} style={{ position: 'absolute', left: '12px', top: '13px', color: 'var(--text-faint)' }} />
              </div>
            </div>

            <button
              type="submit"
              className="btn btn-primary"
              style={{ width: '100%', marginTop: '8px', padding: '12px' }}
              disabled={loading}
            >
              {loading ? 'Signing in…' : 'Sign In'}
            </button>
          </form>
        ) : (
          /* Register Form — all fields required with red star */
          <form onSubmit={handleRegisterSubmit}>
            <div className="form-group">
              <label className="form-label">Full Name {requiredStar}</label>
              <input
                type="text"
                className="form-input"
                style={{ width: '100%', borderColor: fieldErrors.fullName ? 'var(--danger)' : undefined }}
                placeholder="Kamal Perera"
                value={fullName}
                onChange={(e) => { setFullName(e.target.value); if (fieldErrors.fullName) setFieldErrors(prev => ({ ...prev, fullName: '' })); }}
                required
              />
              {fieldErrors.fullName && <div style={fieldErrorStyle}>{fieldErrors.fullName}</div>}
            </div>

            <div className="form-group">
              <label className="form-label">Select Account Role {requiredStar}</label>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px' }}>
                <button
                  type="button"
                  onClick={() => setRegRole('Farmer')}
                  className={`btn ${regRole === 'Farmer' ? 'btn-primary' : 'btn-secondary'}`}
                  style={{ padding: '10px' }}
                >
                  Farmer
                </button>
                <button
                  type="button"
                  onClick={() => setRegRole('Buyer')}
                  className={`btn ${regRole === 'Buyer' ? 'btn-primary' : 'btn-secondary'}`}
                  style={{ padding: '10px' }}
                >
                  Buyer
                </button>
              </div>
            </div>

            <div className="form-group">
              <label className="form-label">Email Address {requiredStar}</label>
              <input
                type="email"
                className="form-input"
                style={{ width: '100%', borderColor: fieldErrors.email ? 'var(--danger)' : undefined }}
                placeholder="kamal@domain.com"
                value={email}
                onChange={(e) => { setEmail(e.target.value); if (fieldErrors.email) setFieldErrors(prev => ({ ...prev, email: '' })); }}
                required
              />
              {fieldErrors.email && <div style={fieldErrorStyle}>{fieldErrors.email}</div>}
            </div>

            <div className="form-group">
              <label className="form-label">Phone Number {requiredStar}</label>
              <input
                type="tel"
                className="form-input"
                style={{ width: '100%', borderColor: fieldErrors.phone ? 'var(--danger)' : undefined }}
                placeholder="07X-XXX XXXX"
                value={phone}
                onChange={(e) => handlePhoneChange(e.target.value)}
                maxLength={12}
                required
              />
              <div style={{ fontSize: '0.7rem', color: 'var(--text-faint)', marginTop: '3px' }}>
                Format: 07X-XXX XXXX (10 digits starting with 07)
              </div>
              {fieldErrors.phone && <div style={fieldErrorStyle}>{fieldErrors.phone}</div>}
            </div>

            <div className="form-group">
              <label className="form-label">Password {requiredStar}</label>
              <input
                type="password"
                className="form-input"
                style={{ width: '100%', borderColor: fieldErrors.password ? 'var(--danger)' : undefined }}
                placeholder="At least 6 characters"
                value={password}
                onChange={(e) => { setPassword(e.target.value); if (fieldErrors.password) setFieldErrors(prev => ({ ...prev, password: '' })); }}
                required
                minLength={6}
              />
              {fieldErrors.password && <div style={fieldErrorStyle}>{fieldErrors.password}</div>}
            </div>

            <div className="form-group">
              <label className="form-label">District {requiredStar}</label>
              <select
                className="form-select"
                style={{ width: '100%', borderColor: fieldErrors.region ? 'var(--danger)' : undefined }}
                value={region}
                onChange={(e) => { setRegion(e.target.value); if (fieldErrors.region) setFieldErrors(prev => ({ ...prev, region: '' })); }}
                required
              >
                <option value="" style={{ background: 'var(--bg-input)' }}>— Select your district —</option>
                {SRI_LANKA_DISTRICTS.map(d => (
                  <option key={d} value={d} style={{ background: 'var(--bg-input)' }}>{d}</option>
                ))}
              </select>
              {fieldErrors.region && <div style={fieldErrorStyle}>{fieldErrors.region}</div>}
            </div>

            <button
              type="submit"
              className="btn btn-primary"
              style={{ width: '100%', marginTop: '8px', padding: '12px' }}
              disabled={loading}
            >
              {loading ? 'Creating...' : 'Register Account'}
            </button>
          </form>
        )}
      </div>
    </div>
  );
};
