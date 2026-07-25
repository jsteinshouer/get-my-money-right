# 12 — Docker Packaging

**What to build:** The whole app runs as a single Docker container on the home network — the API serves the built React client's static files directly, with the SQLite database persisted via a mounted volume.

**Blocked by:** 06 (Budget vs. Actual View), 07 (Tagging Transactions), 10 (CSV Import Confirm & Dedupe), 11 (Category Spend Trend Report)

**Status:** ready-for-agent

- [ ] Multi-stage Dockerfile: Node stage builds the React client, .NET stage publishes the API, final runtime stage copies the client build into the API's static file root and serves both API and SPA from one process/port
- [ ] SQLite file path is configurable via env var/appsettings, documented volume mount
- [ ] Cookie/auth settings confirmed correct for plain-HTTP LAN use (no HTTPS redirection/HSTS, matching ticket 01's config)
- [ ] `docker build` + `docker run -p 8080:8080 -v ./data:/app/data` succeeds; the app is reachable from another device on the LAN
- [ ] Restarting the container preserves data (verified manually: stop, start, confirm previously entered data is still present)
