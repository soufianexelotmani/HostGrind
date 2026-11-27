# Azure Free First — déploiement économique

Objectif : développer entièrement en local avant d'utiliser l'essai Azure (offre mentionnée 200 USD/30 jours et services gratuits éligibles). Aucun coût zéro garanti. Vérifier conditions actuelles, éligibilité, région, quotas, runtime .NET 10 et tarifs dans portail/documentation officielle avant provisionnement. Ne jamais passer automatiquement à Pay-As-You-Go ni désactiver limite de dépenses. Budget alerts ne bloquent pas les dépenses.

## Local
SQL Server Developer (dev/test uniquement) ou Express, fichiers privés hors webroot, Mailpit SMTP, .NET User Secrets/env vars, Angular proxy /api. Docker Compose facultatif pour SQL/Mailpit, volumes persistants. Aucun secret dans .env.example. Staging/Production configs séparées, données fictives hors prod.

## Cible initiale
Un App Service hébergeant Angular build dans wwwroot et API ASP.NET Core (même origine, fallback SPA hors /api). F1 si compatible et disponible pour démonstration, Azure SQL Database free si éligible avec pause/arrêt à épuisement quota si disponible, Azure Files privé persistant via IFileStorageService, Key Vault + Managed Identity lorsque supportés, Application Insights/Monitor avec sampling et limites, SMTP MailKit configurable. Sous-domaine Azure HTTPS. Ne pas supposer montage Azure Files compatible F1 : vérifier ; si impossible, garder démo locale ou proposer stockage privé alternatif/plan supérieur avec validation. Jamais stocker documents réels sur volume éphémère. Pas de staging cloud permanent au départ ; créer à la demande si budget et accord. F1 peut dormir, Hosted Services non garantis 24/7 ; jobs SQL durables, rattrapage, afficher dernière exécution. Pour production réelle, exiger plan adapté à disponibilité, stockage, sécurité et sauvegardes.

## Bicep et CI/CD
Bicep paramétrable (region, SKU, noms, tags, budget), defaults économiques, ressources optionnelles désactivées si coût inconnu. Générer scripts validation/what-if et teardown limité au resource group de test, avec confirmation explicite ; jamais exécuter automatiquement. GitHub Actions PR restore/npm ci, lint/analyzers, tests, scans, build ; artefact Angular+.NET ; déploiement staging sur demande, smoke tests, approbation manuelle production. Azure OIDC, permissions minimales, secrets via Key Vault/config sécurisée. Migrations EF contrôlées avec sauvegarde/plan rollback ; pas de migration automatique au boot. Vérifier health, login, stockage, jobs, isolation après déploiement. Ne pas exposer SQL publiquement sans restriction réseau appropriée.

## Exploitation
Objectifs sauvegardes SQL et documents 30 jours, RPO 24h, RTO 4h, à vérifier selon SKU ; tester restauration coordonnée DB/fichiers. Logs structurés, alertes erreurs/latence/DB/jobs/stockage/budget, dashboard Admin réel. Limiter ingestion et rétention, aucun PII inutile. Vérifier loi marocaine 09-08, CNDP, région et transferts internationaux, conservation et confidentialité avant données réelles. Aucun domaine acheté, aucune licence payante sans accord.

## Checklist avant essai
Build/test local validés ; estimation Azure Pricing Calculator et quotas vérifiés ; région et conformité validées ; plan stockage persistant testé ; secrets et MFA configurés ; budget/alertes et limite de dépenses ; sauvegarde/restauration testées ; autorisation utilisateur explicite. Si un point bloque, fournir plan alternatif local et ne pas provisionner. Planifier semaine 1 déploiement, 2 tests, 3 recette, 4 coûts et décision de poursuite. Documenter ressources réellement créées et coûts observés, jamais inventer.
