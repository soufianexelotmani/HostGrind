# HostGrid authentication

## Local setup

Apply the existing EF migrations to the local development catalog before signing in. Set the connection string as shown in the [database guide](DATABASE.md). Start [Mailpit](https://mailpit.axllent.org/) with `mailpit --listen 127.0.0.1:8025 --smtp 127.0.0.1:1025` and open its local inbox on port 8025. The API defaults to loopback SMTP in Development. The browser uses the Angular origin `http://localhost:4200` and the existing relative `/api` proxy.

Use an environment variable or .NET User Secrets for the signing key. To configure a stable local key in PowerShell:

```powershell
$bytes = New-Object byte[] 32
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
dotnet user-secrets set --project backend 'Security:SigningKey' ([Convert]::ToBase64String($bytes))
```

An unconfigured key in Development is ephemeral. Production requires a configured random signing key and the HTTPS public origin. Configure SMTP through `Email:Host`, `Email:Port`, `Email:From`, and optional `Email:Username` and `Email:Password`.

Bootstrap the first Super Admin from a trusted terminal with database access. Use a fictitious address with Mailpit for local testing. Set `Security__BootstrapEmail` to that address and run:

```powershell
dotnet run --project backend --launch-profile http -- --bootstrap-super-admin
```

The command creates a pending account and sends an activation email. If delivery fails, repeat with the same pending address. The command stops after the first administrator activates. There is no public registration. Open the invitation, choose a password of at least 12 characters, sign in, and enroll an authenticator before any admin session is issued.

## Browser flows

- Sign-in checks the password and account status. An account with required or enabled MFA receives a short-lived verification challenge. The API issues the session after a valid authenticator or recovery code.
- Enrollment shows a manual time-based setup key. Enter the current six-digit code to confirm. Keep the ten recovery codes in a secure location. Each recovery code works once.
- Reset links expire after 30 minutes and become invalid after use. Password reset preserves configured MFA and revokes current sessions. Password change also revokes sessions.
- MFA reset requires a password and either the current authenticator code or an unused recovery code. Enrollment becomes mandatory again at the next sign-in for managers and administrators. Losing the authenticator and every recovery code requires an operator recovery process.
- Session restoration calls refresh when Angular starts. Angular keeps the access token in memory and coalesces concurrent refresh requests. Where the browser exposes Web Locks, tabs serialize refresh and logout using the same lock.

## API routes

All paths below are under `/api/v1`. Unsafe requests require `X-HostGrid-CSRF: 1`. Browser requests must originate from the configured frontend. Protected endpoints accept the access token as `Authorization: Bearer …`.

| Method | Path | Purpose |
|---|---|---|
| POST | `/auth/login` | Verify password and return the next step |
| POST | `/auth/refresh` | Rotate the cookie and restore a session |
| POST | `/auth/logout` | Revoke the current session family and clear the cookie |
| GET | `/auth/me` | Read the current account and active scopes |
| POST | `/auth/activate` | Consume invitation and set the initial password |
| POST | `/auth/password/reset-request` | Request a password reset email |
| POST | `/auth/password/reset` | Consume a reset link |
| POST | `/auth/password/change` | Verify current password and replace it |
| POST | `/auth/mfa/start` | Start optional MFA enrollment for an authenticated owner |
| POST | `/auth/mfa/enrollment` | Retrieve setup details for a valid challenge |
| POST | `/auth/mfa/verify` | Verify TOTP or redeem a recovery code |
| POST | `/auth/mfa/reset` | Revoke sessions and rotate the authenticator key |
| GET, POST | `/admin/organizations` | List or create organizations as Super Admin |
| GET | `/admin/security-events` | Read paginated platform security events as Super Admin |
| POST | `/admin/accounts/{userId}/disable` | Disable an account globally as Super Admin |
| POST | `/admin/organizations/{organizationId}/managers/{userId}` | Assign an active account to another organization as Manager |
| GET | `/organizations/{organizationId}/accounts` | List accounts the caller can manage |
| POST | `/organizations/{organizationId}/accounts/invitations` | Invite a manager or owner within policy |
| PUT | `/organizations/{organizationId}/accounts/{userId}/membership` | Change or deactivate a scoped membership |
| GET | `/organizations/{organizationId}/properties/{propertyId}/access` | Check access against a real tenant property |

The Angular foundations include role landing pages and self-service security. Operational screens, reports, owner requests, and administration screens belong to their respective phases.
