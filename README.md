# UrlShortenerMvc

A production-style URL shortener built with **ASP.NET Core 10 MVC**. Users can shrink long links (anonymously or signed in), and authenticated users get a dashboard with per-link click analytics — clicks over time, referrers, countries, devices, and browsers.

**Live demo:** [short.runasp.net](https://short.runasp.net)

---

## Features

- **Shorten any URL** — anonymous or signed in, with server-side validation (scheme allow-list, embedded-credential rejection, self-referential-domain blocking, URL normalization).
- **Custom expiration** — links can be set to expire in 1, 7, or 30 days, or never.
- **Link management dashboard** — search, paginate, edit, enable/disable, or soft-delete your links.
- **Per-link analytics** — clicks over time (chart), top referrers, top countries, device breakdown, and browser breakdown, computed from raw click events.
- **Fast redirects** — the redirect path reads from a Redis cache first and only falls back to SQL Server on a miss, with cache invalidation on disable/delete.
- **Click tracking off the hot path** — every redirect enqueues a click event into an in-memory channel; a background worker drains it and writes to the database asynchronously, so a database hiccup never slows down a redirect.
- **Authentication** — ASP.NET Core Identity ("Individual Accounts") with email confirmation, plus Google external login.
- **Abuse protection** — per-endpoint rate limiting: a global per-IP limiter, a tighter limiter on link creation (5/min anonymous, 10/min authenticated), and a separate limiter on the redirect endpoint.
- **GeoIP** — click country is resolved locally via MaxMind's GeoLite2 country database (no external API call per click).

---

## Tech Stack

| Layer           | Technology                                                                         |
| --------------- | ---------------------------------------------------------------------------------- |
| Framework       | ASP.NET Core 10 MVC (.NET 10)                                                      |
| Auth            | ASP.NET Core Identity (Individual Accounts) + Google OAuth                         |
| Database        | SQL Server, via EF Core                                                            |
| Cache           | Redis (Upstash in production), via `IDistributedCache` / `StackExchangeRedisCache` |
| Click ingestion | `System.Threading.Channels` in-process queue + a `BackgroundService` worker        |
| Email           | Brevo SMTP via MailKit                                                             |
| GeoIP           | MaxMind GeoLite2 (`.mmdb`, bundled locally)                                        |
| Charts          | Chart.js                                                                           |
| CI/CD           | GitHub Actions → Web Deploy to MonsterASP.NET                                      |

---

## Architecture

### Redirect path (cache-aside)

```
GET /{shortCode}
  │
  ├─ Redis GET link:{shortCode}
  │     HIT  → enqueue click event → 302 redirect
  │
  └─ MISS → query SQL (active, not expired)
             found → cache 10 min TTL → enqueue click event → 302 redirect
             not found / expired → 404
```

Cache entries are written with a 10-minute absolute TTL, and are explicitly evicted from Redis the moment a link is disabled or deleted — the app doesn't rely on TTL expiry alone to keep the cache correct.

### Click tracking pipeline

Rather than writing to SQL Server synchronously on every redirect, `RedirectController` pushes a `ClickEvent` (link ID, timestamp, referrer, user agent, GeoIP country) onto a bounded, in-memory `Channel`. A single-reader `ClickWorker` background service drains the channel and, per event, inserts the click row and increments the link's denormalized `ClickCount` inside one transaction. This keeps the redirect response fast and decoupled from database latency.

### Analytics

`LinksController.Details` pulls raw click rows for a link (filtered by a date range) and aggregates them in memory into: clicks-per-day, top referrers (grouped by host), top countries (via GeoIP-resolved ISO codes), and device/browser breakdowns (parsed from the User-Agent string) — rendered with Chart.js.

---

## Project Structure

```
UrlShortenerMvc/
├── Controllers/
│   ├── HomeController.cs
│   ├── LinksController.cs        # dashboard, create/edit/disable/delete, analytics
│   └── RedirectController.cs     # the /{shortCode} hot path
├── Services/
│   ├── LinkService.cs            # short-code creation, listing, click queries
│   ├── ShortCodeGenerator.cs     # cryptographically random Base62 codes (6–8 chars)
│   ├── UrlValidationService.cs   # scheme/host/credential validation + normalization
│   ├── GeoIpService.cs           # MaxMind GeoLite2 country lookup
│   ├── BrevoEmailSender.cs       # Identity IEmailSender over SMTP
│   └── ClickTracking/
│       ├── ClickQueue.cs         # bounded Channel<ClickEvent>
│       └── ClickWorker.cs        # BackgroundService that flushes to SQL
├── Models/                       # Link, Click, User (IdentityUser<Guid>)
├── ViewModels/
├── Data/                         # ApplicationDbContext + EF Core migrations
├── DependencyInjection.cs        # DI, Identity, Redis, Google auth, rate limiting
└── Program.cs
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB is fine for development)
- A Redis instance (local `redis` container, or a free [Upstash](https://upstash.com) database)
- A Brevo (or other SMTP) account for outgoing email
- A Google OAuth client ID/secret (optional, for Google sign-in)

### Configuration

Copy the shape below into `appsettings.json` or, better, into [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) so real credentials never get committed.

### Run locally

```bash
git clone https://github.com/kerolesnabiel/UrlShortenerMvc.git
cd UrlShortenerMvc/UrlShortenerMvc
dotnet ef database update
dotnet run
```

The app will be available at the URL shown in the console (see `Properties/launchSettings.json`).

---

## Rate Limits

| Endpoint             | Anonymous             | Authenticated          |
| -------------------- | --------------------- | ---------------------- |
| Create link          | 5 / minute (per IP)   | 10 / minute (per user) |
| Redirect (`/{code}`) | 60 / minute (per IP)  | 60 / minute (per IP)   |
| Global               | 120 / minute (per IP) | 120 / minute (per IP)  |

---

## Deployment

The `main` branch auto-deploys via [`.github/workflows/deploy.yml`](.github/workflows/deploy.yml): GitHub Actions restores, builds, tests, and publishes the app, then pushes it to [MonsterASP.NET](https://monsterasp.net) using Web Deploy. The live instance runs at [short.runasp.net](https://short.runasp.net).
