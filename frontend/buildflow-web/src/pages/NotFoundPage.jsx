import { Link } from 'react-router-dom'
export default function NotFoundPage() { return <main className="simple-page"><span className="error-code">404</span><h1>Page not found</h1><p>The page may have moved or does not exist.</p><Link className="button button-primary" to="/">Go home</Link></main> }
