# 01 — User Login

**What to build:** Two household members can each log in with their own account and reach an authenticated area of the app. Sessions are cookie-based and work over plain HTTP (LAN-only, no TLS).

**Blocked by:** None — can start immediately

**Status:** ready-for-agent

- [ ] ASP.NET Core Identity is wired up with a single shared `BudgetDbContext : IdentityDbContext<ApplicationUser>` (SQLite)
- [ ] Two household user accounts are seeded (fixed seed data, not open registration)
- [ ] Login, logout, and "current user" (me) endpoints exist and are covered by `WebApplicationFactory` integration tests
- [ ] Cookie auth is configured for plain-HTTP LAN use (`CookieSecurePolicy.None`/`SameAsRequest`, no HTTPS redirection/HSTS)
- [ ] React login page authenticates against the API and lands on an authenticated shell/nav
- [ ] Unauthenticated requests to protected endpoints return 401; a logged-in session returns 200
- [ ] Playwright e2e test: log in as each seeded user and reach the authenticated shell
