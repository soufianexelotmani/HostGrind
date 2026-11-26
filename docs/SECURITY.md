# HostGrid security model

## Authorization and tenancy

Super Admin is a global Identity role. Manager and Owner permissions come from active database memberships in an active organization. An Owner scope also requires an active Owner record linked to the account. JWT validation reloads the account, security stamp, active session, and effective memberships for each protected request. Tenant selection uses the named organization in the route and records explicit administrator access. The EF filters require an authorized tenant, and composite foreign keys guard against cross-tenant references at the database level.

Managers can list and manage Owner memberships within their own scopes. They cannot assign Manager or Super Admin. Organization administrators use explicit Super Admin endpoints. Owner property access checks the verified Owner ID against the persisted property Owner ID. Future operational, finance, report, and request endpoints must apply the same server-side policies at their resource boundaries.

## Sessions and credentials

JWT access tokens use a 15-minute lifetime, explicit issuer and audience, and HS256 with a secret of at least 32 random bytes. Refresh cookies have a seven-day lifetime, are HttpOnly and SameSite=Strict, and require Secure outside local development HTTP. Refresh tokens and pending login challenges are hashed before persistence. SQL Server transaction-owned locks serialize refresh and account-security mutations for a user. Reuse of a replaced cookie revokes the session family, including a replacement already delivered to another request. Browsers without Web Locks coordinate only within each tab. If concurrent tabs submit the same cookie, the user signs in again.

UserManager enforces a 12-character minimum and a 15-minute lock after five password or MFA failures. Recovery codes contain 96 bits of randomness, are stored as hashes, and are consumed once. Previously verified TOTP codes are rejected within the acceptance window. Authenticator setup keys are returned only for a valid pending enrollment over the auth endpoint. Protect the database and backups, which contain Identity authenticator keys and security state.

## HTTP and auditing

Production uses HTTPS redirection and HSTS. Configure `Security:PublicOrigin` to the exact HTTPS frontend origin. Same-origin hosting and the local Angular proxy need no cross-origin grants. Unsafe requests require a custom header, reject cross-site fetch metadata, and validate the Origin when supplied. API responses have CSP, framing restrictions, MIME-sniffing protection and a no-referrer policy. The per-IP limiter applies to security endpoints.

Security audit rows store action, actor and subject identifiers when known, authorized organization, UTC time, and a correlation identifier. They exclude emails, passwords, raw token values, recovery codes, and MFA secrets. Explicit admin routes govern global reads. Security failures use ProblemDetails and avoid returning underlying exception messages.

## Deployment and remaining boundaries

For multiple application instances, use a shared, protected ASP.NET Core Data Protection key ring and synchronize clocks. The built-in rate limiter is per instance. Configure trusted forwarded headers and an edge limiter when deploying behind a reverse proxy. Direct SMTP delivery supports local Mailpit and production STARTTLS. Durable mail retries and retention policies are later-phase operations. Protect Key Vault or equivalent secret-store access, restrict application database permissions, and test alerting and backups before a real rollout.

Account recovery for lost MFA requires a recovery code. Define a verified operator recovery procedure before using the system for real accounts. The financial and reservation decisions listed in the [database guide](DATABASE.md) remain open.
