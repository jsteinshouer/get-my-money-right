# 01 — User Login

**What to build:** Two household members can each log in with their own account and reach an authenticated area of the app. Sessions are cookie-based and work over plain HTTP (LAN-only, no TLS).

**Blocked by:** None — can start immediately

**Status:** done

- [x] ASP.NET Core Identity is wired up with a single shared `BudgetDbContext : IdentityDbContext<ApplicationUser>` (SQLite)
- [x] Two household user accounts are seeded (fixed seed data, not open registration)
- [x] Login, logout, and "current user" (me) endpoints exist and are covered by `WebApplicationFactory` integration tests
- [x] Cookie auth is configured for plain-HTTP LAN use (`CookieSecurePolicy.None`/`SameAsRequest`, no HTTPS redirection/HSTS)
- [x] React login page authenticates against the API and lands on an authenticated shell/nav
- [x] Unauthenticated requests to protected endpoints return 401; a logged-in session returns 200
- [x] Playwright e2e test: log in as each seeded user and reach the authenticated shell

## Comments

Implemented as the greenfield scaffolding for the whole app (Phase 1 of the build order in the implementation plan), since this was the first ticket and nothing existed yet.

**What was built:**
- `budget.slnx` solution with `src/Api` (ASP.NET Core Minimal API, REPR pattern) and `tests/Api.Tests` (xUnit).
- `Api.Data.ApplicationUser : IdentityUser` (adds a required `DisplayName`) and `Api.Data.BudgetDbContext : IdentityDbContext<ApplicationUser>` (SQLite), with an `InitialCreate` EF Core migration applied automatically at startup via `Database.MigrateAsync()`.
- `Api.Features.Identity` area: `Login` (`POST /api/identity/login`), `Logout` (`POST /api/identity/logout`), `Me` (`GET /api/identity/me`, `RequireAuthorization()`), each as Request/Handler/Response + Mapperly `Mapper`, with a FluentValidation `Validator` on `Login`. Login/logout use the "expected failure" Operation Result pattern (401 for bad credentials) rather than exceptions; `ForEvolve.ExceptionMapper` is wired for truly exceptional cases (e.g. `DbUpdateException` → 409) and already produces consistent `ProblemDetails` for auth failures on protected routes.
- Cookie auth configured for plain-HTTP LAN use: `CookieSecurePolicy.SameAsRequest` (the safer of the two allowed options — still works with no TLS), `OnRedirectToLogin`/`OnRedirectToAccessDenied` overridden to return 401/403 instead of redirecting (this API has no login *page* to redirect to), no `UseHttpsRedirection()`/HSTS, and the `https` launch profile was removed entirely.
- Two household users are seeded from a `HouseholdUsers` config section (`appsettings.json`, overridable via env vars) rather than hardcoded — placeholder emails/passwords (`user1@household.local` / `ChangeMe123!`, `user2@household.local` / same) are committed since there's no real household member data yet; **these should be changed via environment variable overrides before this ever leaves localhost.**
- `tests/Api.Tests/Fixtures/BudgetApiFactory.cs`: a `WebApplicationFactory<Program>` against a real, kept-open in-memory SQLite connection (not the EF Core InMemory provider), per the spec's testing decision that unique constraints need to be actually enforced. 8 gray-box tests across `Identity.Login.Tests.cs`, `Identity.Me.Tests.cs`, `Identity.Logout.Tests.cs` cover valid/invalid login, validation (400), unauthenticated `/me` (401), authenticated `/me` (200), and logout ending the session.
- `client/`: Vite + React 19 + TypeScript + Pico.css (classless). `AuthProvider`/`useAuth` context backed by `GET /api/identity/me` on load; `RequireAuth` route guard redirects to `/login`; `LoginPage` + `ShellPage` (nav showing the signed-in user's display name + logout). Dev proxy (`vite.config.ts`) forwards `/api` to the API on `http://localhost:5059` so the browser sees one origin, matching the eventual single-container deployment.
- `client/e2e/login.spec.ts` (Playwright): logs in as each seeded user and asserts the authenticated shell renders, plus unauthenticated-redirect and logout-returns-to-login cases. `playwright.config.ts` boots both the API and the Vite dev server as `webServer` entries.

**Deviations / notes:**
- Route prefix is `/api/*` rather than bare `/*`, anticipating ticket 12 (single container serving both the SPA static files and the JSON API from one process) — this avoids a route collision between client-side routes and API routes.
- Package correction from the REPR reference doc: current FluentValidation minimal-API auto-validation comes from `SharpGrip.FluentValidation.AutoValidation.Endpoints` (`AddFluentValidationAutoValidation()`), not `FluentValidation.AspNetCore` (MVC-only/deprecated for this purpose) — verified against the library's current README before wiring it up.
- Pinned `SQLitePCLRaw.bundle_e_sqlite3` to 3.0.4 directly; the version EF Core Sqlite 10.0.10 pulls transitively (2.1.11) has a known high-severity advisory.
- `react-router-dom` is pinned at the latest published 7.18.1. `npm audit` still flags one high-severity advisory (RSC-mode CSRF bypass) with no fixed version published yet — it's specific to React Server Components/framework mode, which this plain client-rendered SPA doesn't use, so it doesn't apply to this deployment. Worth a quick recheck (`npm audit`) next time this package is touched.
- This sandbox had no root/sudo, so `playwright install --with-deps` couldn't install its OS-level shared libraries (`libnspr4`, `libnss3`, `libasound2`, etc.); they were fetched via `apt-get download` + `dpkg-deb -x` (no root required) into a scratch dir and loaded via `LD_LIBRARY_PATH` to run the e2e suite once in this session. This is a one-time environment quirk, not something committed to the repo — `npx playwright install --with-deps` should work normally with the user's own permissions.

**How to verify:**
1. `dotnet test tests/Api.Tests` — 8/8 pass (real SQLite-backed gray-box tests).
2. `cd src/Api && dotnet run` (defaults to `http://localhost:5059`), then in another terminal `cd client && npm run dev` and open `http://localhost:5173` — you'll land on `/login`; sign in as `user1@household.local` / `ChangeMe123!` (or `user2@household.local` / `ChangeMe123!`) and you should land on the shell showing "Welcome, Household Member 1/2" with a working Log out button.
3. `curl -i http://localhost:5059/api/identity/me` with no cookie → 401; after logging in via the login endpoint and replaying the cookie → 200.
4. `cd client && npm run test:e2e` (requires `npx playwright install chromium` once) — runs the 4 Playwright specs against a real API + Vite dev server pair.

Landed on `main` in commit(s) following this one.
