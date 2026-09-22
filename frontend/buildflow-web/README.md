# BuildFlow AI web

Run `npm ci`, then `npm run dev`. Validation uses `npm run lint`, `npm test`, and
`npm run build`.

The web app provides public landing, login, and registration pages plus a protected
dashboard shell. Authentication state restores on startup, access tokens refresh
automatically, and navigation is derived from API roles. "Keep me signed in" uses
local storage; otherwise the session ends when the browser session closes.

Copy `.env.example` to `.env` to override the API URL. Vite variables are public,
so they must never contain secrets.
