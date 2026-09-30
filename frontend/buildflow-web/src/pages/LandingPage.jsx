export default function LandingPage() {
  return (
    <main className="landing-main">
      <section className="hero-section">
        <div className="hero-copy">
          <span className="badge">AI-powered construction operations</span>
          <h1>Build smarter. Coordinate faster.</h1>
          <p>Connect site teams, materials, procurement, workforce and intelligent planning in one secure platform.</p>
          <div className="hero-actions"><a className="button button-primary" href="/register">Create account</a><a className="button button-secondary" href="#platform">Explore platform</a></div>
          <small>Projects · Inventory · Procurement · Scheduling · Agentic AI</small>
        </div>
        <div className="product-preview" aria-label="BuildFlow operations preview">
          <div className="preview-head"><span>Site overview</span><span className="status-dot">Live</span></div>
          <div className="metric-row"><article><small>Progress</small><strong>68%</strong></article><article><small>Pending plans</small><strong>04</strong></article></div>
          <div className="preview-card"><span className="preview-icon amber">!</span><div><strong>Inventory review</strong><small>12 cement bags below requirement</small></div><span className="status warning">Review</span></div>
          <div className="preview-card"><span className="preview-icon green">✓</span><div><strong>Workforce schedule</strong><small>Ground floor crew allocated</small></div><span className="status success">Ready</span></div>
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
