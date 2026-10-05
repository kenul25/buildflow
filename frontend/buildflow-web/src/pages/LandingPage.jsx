import './landing-hero.css'

export default function LandingPage() {
  return (
    <main className="landing-main">
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
          ].map(([title, copy], index) => <article key={title}><span>0{index + 1}</span><h3>{title}</h3><p>{copy}</p></article>)}
        </div>
      </section>
      <section className="workflow-section" id="workflow"><span className="eyebrow">Controlled intelligence</span><h2>From site request to approved execution plan</h2><div className="workflow-row">{['Site request', 'Planning', 'Inventory', 'Procurement', 'Validation', 'Manager approval'].map((step) => <span key={step}>{step}</span>)}</div><p>AI prepares recommendations. Business rules validate them. Authorized managers remain in control.</p></section>
    </main>
  )
}
