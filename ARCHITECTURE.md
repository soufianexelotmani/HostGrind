# Architecture technique

## Organisation
```text
HostGrid/
  backend/HostGrid.Api.csproj
  backend/Controllers/
  backend/Services/{Interfaces,Implementations}/
  backend/Repositories/{Interfaces,Implementations}/
  backend/Entities/                 # sous-dossiers métier
  backend/DTOs/                     # sous-dossiers métier
  backend/Mappers/
  backend/Validators/
  backend/Data/{Configurations,Seed,Migrations}/
  backend/Enums/
  backend/Middlewares/
  backend/Exceptions/
  backend/Security/
  backend/BackgroundServices/
  backend/Integrations/{Email,Storage}/
  backend/Extensions/
  backend/Options/
  backend/Common/{Pagination,Constants,Helpers}/
  backend/Resources/
  frontend/src/app/{core,features,shared,layouts}/
  tests/                            # projets xUnit séparés
  docs/
  deploy/
  .github/workflows/
```
Conserver solution .sln ou .slnx existante. Modules logiques Identity, Organization, Owners, Properties, Reservations, Calendar, Finance, Expenses, Payouts, Operations, Requests, Documents, Reporting, Notifications, Administration, Audit. Controllers -> services -> repositories -> EF Core ; mappers sans logique métier. Un HostGridDbContext dérivé IdentityDbContext, configurations Fluent API par module. Pas de couche Domain en projet séparé ; règles pures testables dans Services/Common ou Entities.

## API
REST `/api/v1`, OpenAPI/Scalar en dev, pagination serveur (page/pageSize, maximum configurable), tri et filtres sur champs autorisés, dates ISO, ProblemDetails (400/401/403/404/409/429). Routes ressources : auth (login, refresh, logout, MFA, reset), organizations, users, owners, properties, contracts, reservations, calendar, payments, refunds, expenses, closures, payouts, cleaning, maintenance, tasks, requests, documents, reports, dashboards, notifications, admin/health, audit. Préférer endpoints d'action explicites pour transitions (`/reservations/{id}/check-out`, `/closures/{id}/close`). Contrats DTOs documentés, pas de mass assignment. RowVersion/ETag pour modifications concurrentes ; idempotency keys pour opérations financières et jobs. Recherche globale limitée aux ressources autorisées. Les rapports utilisent les mêmes services financiers que dashboards.

## Frontend
Feature-based, routes lazy, guards auth/role (UX seulement), interceptors auth/error, refresh single-flight, services HTTP typés, Signals pour état local/partagé, RxJS pour flux, pas de NgRx obligatoire. Features auth, dashboard, properties, owners, reservations, calendar, finance, expenses, payouts, cleaning, maintenance, tasks, requests, reports, notifications, admin, settings. Shared composants, pipes, directives, utilitaires. Proxy Angular `/api` en dev, même origine en production. Aucun calcul financier client faisant autorité.

## Sécurité
Identity email/password, minimum 12 caractères, verrouillage 5 échecs/15 min, reset email avec tokens à durée limitée, invitation/activation manuelle. MFA TOTP obligatoire Admin/Manager, optionnelle Owner ; récupération sécurisée. JWT 15 min signé, validation issuer/audience/expiry, access token en mémoire ; refresh 7 jours, cookie HttpOnly Secure SameSite adapté, hash en base, rotation atomique, détection réutilisation, révocation famille/session. CSRF sur endpoints cookie, HTTPS/HSTS, CSP, rate limiting, CORS restrictif si nécessaire. Logout révoque refresh ; access token court et contrôle de révocation pour actions sensibles. Pas de secret en config versionnée.
TenantContext résolu depuis identité et membership vérifié ; query filters EF + autorisation par ressource + FK composites. Admin global explicite, manager uniquement son organisation et gestion des comptes Owner, jamais élévation de privilèges. Owner lecture de ses ressources, demandes et messages, rapports propres, profil. Pas de modification financière par Owner. Clôture immuable pour tous. Audit anciennes/nouvelles valeurs filtrées, acteur, date, tenant, corrélation. Logs sans tokens, mots de passe ni documents.

## Fichiers, jobs et cache
IFileStorageService local hors wwwroot, identifiants opaques, métadonnées SQL, téléchargement autorisé, contrôle taille/type/signature, quarantaine et analyse avant publication ; aucun fichier exécutable. IEmailService MailKit SMTP, Mailpit en dev, outbox persistante. Hosted Services créent scopes DI, traitent jobs SQL durables avec leases, retries, idempotence, rattrapage au démarrage et gestion multi-instance. Pas de Hangfire. IMemoryCache uniquement pour données appropriées, clés tenant/user/scope, invalidation après mutation ; jamais source de vérité financière. Health checks DB/storage/jobs/email, détails réservés Admin.
