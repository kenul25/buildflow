import { Link } from 'react-router-dom'
export default function UnauthorizedPage() { return <main className="simple-page"><span className="error-code">403</span><h1>Access is restricted</h1><p>Your account does not have permission to open this area.</p><Link className="button button-primary" to="/dashboard">Return to dashboard</Link></main> }
