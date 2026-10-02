import { Link } from 'react-router-dom'
import { Sprout } from '../components/Icons'
import './landing.css'

const STEPS = [
  {
    n: '1',
    title: 'List or browse',
    text: 'Farmers list produce with AI-assisted fair-price guidance. Buyers browse quality-verified listings.',
  },
  {
    n: '2',
    title: 'Order with reserved stock',
    text: 'The moment a buyer orders, the quantity is reserved so the same stock can never be sold twice.',
  },
  {
    n: '3',
    title: 'Officer review & pickup slot',
    text: 'A collection-centre officer reviews the order and confirms a conflict-free pickup slot.',
  },
  {
    n: '4',
    title: 'Track to completion',
    text: 'Follow every order from Pending to Completed, with a notification at each step.',
  },
]

const ROLES = [
  {
    title: 'Farmers',
    points: ['List produce in minutes', 'See a fair-price range before you set yours', 'Track orders placed on your listings'],
  },
  {
    title: 'Buyers',
    points: ['Browse verified, published listings', 'Order and reserve stock instantly', 'Follow pickup scheduling live'],
  },
  {
    title: 'Collection-centre officers',
    points: ['Review orders for your centre', 'Approve conflict-free pickup slots', 'Analytics, quality inspections & reports'],
  },
]

/** Public landing page for signed-out visitors: what AgriConnect does and how an order works. */
export function LandingPage() {
  return (
    <div className="lp">
      <header className="lp-nav">
        <Link to="/" className="lp-brand">
          <span className="lp-logo">
            <Sprout size={20} />
          </span>
          AgriConnect
        </Link>
        <nav className="lp-nav-links" aria-label="Sections">
          <a href="#how">How it works</a>
          <a href="#who">Who it’s for</a>
        </nav>
        <div className="lp-nav-actions">
          <Link to="/login" className="lp-btn lp-btn-ghost">
            Sign in
          </Link>
          <Link to="/login?mode=register" className="lp-btn lp-btn-primary">
            Get started
          </Link>
        </div>
      </header>

      <main>
        <section className="lp-hero">
          <span className="lp-pill">Smart agriculture marketplace</span>
          <h1>
            Fresh produce, <span>fair prices</span>, hassle-free pickup.
          </h1>
          <p>
            AgriConnect connects farmers and buyers, reserves stock the moment an order is placed, and schedules
            pickup through your nearest collection centre.
          </p>
          <div className="lp-hero-actions">
            <Link to="/login?mode=register" className="lp-btn lp-btn-primary lp-btn-lg">
              Create a free account
            </Link>
            <Link to="/login" className="lp-btn lp-btn-ghost lp-btn-lg">
              Sign in
            </Link>
          </div>
        </section>

        <section id="how" className="lp-section">
          <h2>How an order works</h2>
          <div className="lp-grid">
            {STEPS.map((s) => (
              <article key={s.n} className="lp-card">
                <span className="lp-step">{s.n}</span>
                <h3>{s.title}</h3>
                <p>{s.text}</p>
              </article>
            ))}
          </div>
        </section>

        <section id="who" className="lp-section">
          <h2>Built for everyone in the chain</h2>
          <div className="lp-grid lp-grid-3">
            {ROLES.map((r) => (
              <article key={r.title} className="lp-card">
                <h3>{r.title}</h3>
                <ul>
                  {r.points.map((p) => (
                    <li key={p}>{p}</li>
                  ))}
                </ul>
              </article>
            ))}
          </div>
        </section>

        <section className="lp-cta">
          <h2>Ready to trade smarter?</h2>
          <Link to="/login?mode=register" className="lp-btn lp-btn-primary lp-btn-lg">
            Get started
          </Link>
        </section>
      </main>

      <footer className="lp-footer">© {new Date().getFullYear()} AgriConnect</footer>
    </div>
  )
}
