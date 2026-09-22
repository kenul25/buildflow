# BuildFlow AI web

Run npm ci, then npm run dev. Validation: npm run lint and npm run build.

The entry point renders a minimal BuildFlow page. Named files in layouts, pages,
services and routes are placeholders unless used by App.jsx. Feature folders and
shared component folders follow the React design plan. Authentication and routing
are not implemented yet.

Copy .env.example to .env when implementing the API client. Client configuration
is public and must never contain secrets.
