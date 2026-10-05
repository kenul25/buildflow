import './landing-hero.css'
import './landing-footer.css'

export default function LandingPage() {
  return (
    <>
    <main className="landing-main" id="top">
      <section className="landing-hero" aria-labelledby="hero-title">
        <div className="landing-hero-copy">
          <span className="landing-hero-badge"><span aria-hidden="true">✦</span> AI-powered construction operations</span>
          <h1 id="hero-title">Build smarter.<br /><span>Coordinate faster.</span></h1>
          <p>Bring your site teams, materials and schedules together. Turn everyday resource requests into clear, manager-approved plans.</p>
          <div className="landing-hero-actions">
            <a className="button button-primary" href="/register">Create account <span aria-hidden="true">↗</span></a>
            <a className="button button-secondary" href="#platform">Explore platform <span aria-hidden="true">→</span></a>
          </div>
          <div className="landing-hero-capabilities" aria-label="Connected operations">
            <span>Site management</span><span>Inventory & procurement</span><span>Workforce & scheduling</span>
          </div>
        </div>
        <div className="landing-hero-visual">
          <div className="landing-hero-photo">
            <img src="/images/buildflow-construction-hero.webp" width="1536" height="1024" fetchPriority="high" decoding="async" alt="Two site engineers reviewing a tablet beside a building under construction" />
            <span className="landing-hero-photo-label"><span aria-hidden="true">●</span> Built for teams on site</span>
          </div>
        </div>
      </section>
      <section className="platform-section" id="platform">
        <span className="eyebrow">One connected platform</span><h2>Every resource. One source of truth.</h2>
        <div className="feature-grid">
          {[
            ['Project & Site Management', 'Track phases, activities, deadlines and site progress.'],
            ['Materials & Inventory', 'Monitor stock, reservations, shortages and movement.'],
            ['Supplier & Procurement', 'Compare quotations, orders and delivery performance.'],
            ['Workforce & Scheduling', 'Coordinate workers, skills, equipment and shifts.'],
          ].map(([title, copy], index) => <article key={title} id={`platform-${index + 1}`}><span>0{index + 1}</span><h3>{title}</h3><p>{copy}</p></article>)}
        </div>
      </section>
      <section className="workflow-section" id="workflow"><span className="eyebrow">Controlled intelligence</span><h2>From site request to approved execution plan</h2><div className="workflow-row">{['Site request', 'Planning', 'Inventory', 'Procurement', 'Validation', 'Manager approval'].map((step) => <span key={step}>{step}</span>)}</div><p>AI prepares recommendations. Business rules validate them. Authorized managers remain in control.</p></section>
    </main>
    <footer className="landing-footer">
      <div className="landing-footer-inner">
        <div className="landing-footer-invitation">
          <div><span className="eyebrow">Build with clarity</span><h2>Keep your next project moving.</h2><p>Connect the people, resources and plans behind your site.</p></div>
          <a className="button button-primary" href="/register">Get started <span aria-hidden="true">↗</span></a>
        </div>
        <div className="landing-footer-grid">
          <div className="landing-footer-brand">
            <a className="brand" href="/" aria-label="BuildFlow AI home"><span className="brand-mark">BF</span><span>BuildFlow <strong>AI</strong></span></a>
            <p>One connected workspace for construction operations, from the first site request to the approved plan.</p>
            <span className="landing-footer-tagline"><span aria-hidden="true">✦</span> AI-assisted. Manager-approved.</span>
          </div>
          <nav className="landing-footer-links" aria-label="Platform features">
            <h3>Platform</h3>
            <a href="#platform-1">Projects & sites</a>
            <a href="#platform-2">Materials & inventory</a>
            <a href="#platform-3">Suppliers & procurement</a>
            <a href="#platform-4">Workforce & scheduling</a>
          </nav>
          <nav className="landing-footer-links" aria-label="Explore BuildFlow">
            <h3>Explore</h3>
            <a href="#platform">Platform overview</a>
            <a href="#workflow">How it works</a>
            <a href="/login">Sign in</a>
            <a href="/register">Create an account</a>
          </nav>
        </div>
        <div className="landing-footer-bottom">
          <small>© {new Date().getFullYear()} BuildFlow AI. All rights reserved.</small>
          <a href="#top">Back to top <span aria-hidden="true">↑</span></a>
        </div>
      </div>
    </footer>
    </>
  )
}
