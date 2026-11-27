# Instructions permanentes pour Codex

- Lire MASTER_PROMPT.md et les six autres spécifications avant de modifier le code. Respecter les projets existants et les changements utilisateur.
- Un seul projet ASP.NET Core applicatif ; pas de Clean Architecture, CQRS, MediatR, microservices, DDD complexe ni generic repository universel. Projets de tests autorisés.
- SOLID, DRY, KISS, YAGNI ; noms anglais explicites, UI traduite ; méthodes courtes, DI, async/await et CancellationToken, nullable reference types, aucune logique métier dans controllers ou mappers.
- DTOs explicites entrée/sortie ; jamais exposer directement les entités EF/Identity. Validation FluentValidation, erreurs ProblemDetails, logs structurés sans secrets ni PII superflue. Ne pas accepter les montants calculés, rôles ou tenant du client comme autorité.
- Repositories spécifiques aux domaines ; DbContext scoped, projections AsNoTracking, pagination bornée, éviter N+1, transactions pilotées par services. Pas de SaveChanges caché dans chaque repository si opération atomique.
- Vérifier tenant et propriétaire à chaque accès, y compris recherche, exports, fichiers, cache et jobs. Ne jamais contourner les filtres sans politique explicite et audit.
- Aucun secret dans Git, aucun token dans localStorage, aucun cache PWA de données privées. Ne jamais exécuter de commande destructive sur données réelles.
- Installer uniquement dépendances compatibles, verrouiller versions, vérifier licences. Ne pas utiliser de composant premium sans autorisation.
- Avant phase : git status et inspection. Après phase : dotnet build/test, npm ci si nécessaire, npm run build, tests frontend adaptés ; documenter résultats réels dans docs/PROGRESS.md. Ne pas ignorer les tests rouges. Préserver le travail existant.
- Aucun provisionnement cloud, paiement, push ou déploiement réel sans accord. Utiliser données fictives en dev/test ; aucun compte production avec mot de passe par défaut.

## Implementation Checkpoints and Session Recovery

- Divide large implementation phases into small, independently verifiable checkpoints.
- Update docs/PROGRESS.md after every completed checkpoint.
- Record modified files, build results, test results, migrations, decisions and remaining tasks.
- Never rely exclusively on conversation history to track implementation progress.
- At the beginning of a new session, inspect Git status, existing code and progress documentation.
- Resume incomplete work instead of recreating existing implementations.
- Preserve all user changes.
- Never mark a feature as complete without verification.
- Stop at a stable checkpoint whenever possible.
- Keep the repository buildable between checkpoints.
- Do not advance to the next major phase without explicit approval.