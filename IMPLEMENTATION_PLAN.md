# Plan d'implémentation progressif

Chaque phase : inspecter repo, annoncer plan, implémenter, compiler/tester, corriger, mettre à jour docs/PROGRESS.md, indiquer limites et checkpoint. Ne pas déclarer une phase terminée si tests critiques échouent. Commencer phase 1 uniquement ; poursuivre sur demande utilisateur ou accord explicite.

| Phase | Livrables | Critères d'acceptation |
|---|---|---|
| 1 Foundation | vérifier squelette, versions, dossiers, dépendances, config, gitignore, README, SQL Server dev Docker ou instance locale, proxy Angular, health | dotnet build et npm run build réussissent ; API/UI démarrent ; aucun secret |
| 2 Database | entités/configurations, migrations, seed, contraintes, ERD | migration sur SQL Server vierge, seed idempotent, FK tenant testées |
| 3 Identity | login, activation/reset, JWT refresh, MFA, roles, tenant context, policies, audit | tests accès croisés, rotation/revocation, MFA, rate limit |
| 4 UI foundation | layout, tokens, i18n, routes, composants, login | FR/EN/AR RTL, thèmes, mobile, clavier, vrais endpoints auth |
| 5 Properties/Owners | CRUD autorisé, contrats, photos, inventaire, tarifs | owner isolé, fichiers privés, archivage et historique taux |
| 6 Reservations | CRUD manuel, calendrier, blocages, paiements, annulations | concurrence SQL, départ/arrivée même jour, historique et remboursements |
| 7 Finance | moteur, charges, récurrences, avances, clôtures, payouts, journal | exemples 14 000 MAD, cas mixtes, cross-month, immutabilité, idempotence |
| 8 Operations | nettoyage, checklists, contacts, maintenance, tâches, demandes | checkout génère une seule tâche, charge liée avec preuve, permissions |
| 9 Dashboards/Reports | KPI, filtres, widgets, recherche globale, PDF/Excel | mêmes totaux backend, scopes corrects, exports FR/EN/AR vérifiés |
| 10 PWA/Notifications | app shell, outbox SMTP, Hosted Services durables | installable, offline sans PII, rattrapage jobs, retries |
| 11 Quality | tests complets, scans, performance, documentation | scénarios critiques verts, objectif Domain logique métier 80 %, défauts rapportés |
| 12 Azure | Bicep, workflows, runbook, smoke tests | validation locale des scripts ; aucun déploiement sans accord ; budget vérifié |

## Phase 6 — Réservations et calendrier

Le détail des dépendances, endpoints, contrôles de sécurité et tests se trouve dans [le plan des checkpoints](docs/PHASE_06_CHECKPOINTS.md). Chaque checkpoint exige une vérification autonome et une approbation avant la suite. Le dépôt HostGrid fait autorité pour les spécifications.

- 6.0 Règles métier et audit technique, sans implémentation.
- 6.1 Disponibilité et protection contre les écritures concurrentes.
- 6.2 Voyageurs et gestion des réservations manuelles, prix acceptés et révisions, confirmation directe et saisie historique avec justificatif privé.
- 6.3 Calendrier global et blocages de dates.
- 6.4 Cycle de vie, arrivée et départ.
- 6.5 Paiements enregistrés, annulations et remboursements.
- 6.6 Consultation des réservations par Owner.
- 6.7 Vérification finale et optimisation.


## Tests
xUnit, Shouldly, NSubstitute, WebApplicationFactory, Testcontainers SQL Server, Coverlet ; Vitest Angular, Playwright E2E, k6 performance sur environnement autorisé. Ne pas utiliser EF InMemory pour prouver contraintes SQL. Couverture 80 % des règles métier (pas de projet Domain séparé). Tests d'intégration réels : isolation tenant/owner y compris exports/fichiers/recherche, concurrence, clôture, refresh, jobs. Tests UI : login, navigation, formulaires, filtres, erreurs, responsive, RTL, PWA. Scans dépendances NuGet/npm, secrets, analyse statique. Tests de charge uniquement local/staging avec données fictives. Mesurer et rapporter résultats, pas de garantie de performance non testée.

## Commandes de référence (adapter au repo)
```powershell
dotnet restore
dotnet build
dotnet test
cd frontend
npm ci
npm run build
```
Ajouter scripts test/lint/e2e documentés selon configuration réelle. Ne pas appliquer migration production automatiquement au démarrage. README doit préciser versions, ports, config SQL/SMTP/storage, création admin sécurisée, migrations, seed dev et lancement. Guide utilisateur par rôle, guide exploitation, ERD, OpenAPI, journal décisions.
