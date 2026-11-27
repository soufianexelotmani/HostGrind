# Règles métier et financières

## Finance — règle prioritaire
NetPlatformRevenue = GrossRevenue - PlatformFees. Trois modes exclusifs : brut + frais réels, brut + taux frais, net direct. En net direct, brut/frais inconnus restent null (pas zéro fictif), aucune double déduction. ManagerCommission = NetPlatformRevenue × taux historique / 100. OwnerProfit = NetPlatformRevenue - ManagerCommission - charges propriétaire. Commission par bien configurable, défaut 20 %, indépendante des charges. Calcul backend decimal, arrondi au centime explicite et déterministe (proposition AwayFromZero), répartir reliquat pour conserver totaux. Aucun calcul JS faisant autorité.

Exemple validé : net 14 000 MAD, charges 100, commission 2 800, bénéfice 11 100. Net 14 000 et charges 500 => bénéfice 10 700, commission 2 800. Si manager encaisse tout et avance 500 personnellement : remboursement 500, manager reçoit 3 300 au total, propriétaire reçoit 10 700. Si propriétaire paie lui-même 500 : propriétaire reçoit 11 200, bénéfice économique 10 700. Si charges payées depuis recettes : manager reçoit 2 800, propriétaire 10 700. Ces exemples supposent encaissement complet et aucun versement précédent.

## Encaissements et soldes
Distinguer revenu acquis, argent encaissé, charge engagée, charge payée, commission due/payée, avance et remboursement, versement propriétaire. Chaque paiement identifie bénéficiaire manager/owner ; chaque charge payée identifie source (manager personnel, propriétaire personnel, recettes locatives). Ne jamais comptabiliser remboursement comme nouvelle charge. Journal des mouvements réels, pas comptabilité double. Solde de règlement calculé à partir des fonds réellement détenus et obligations : ventiler paiements reçus directement par owner, commissions dues, avances non remboursées, dépenses déjà réglées, versements antérieurs ; ne pas payer des fonds non encaissés. Afficher bénéfice et solde séparément. Pour cas mixtes, implémenter un tableau de rapprochement testable ; demander validation si une règle de règlement reste ambiguë. Aucun transfert bancaire automatique. Versements manuels avec montant, date, mode, référence, allocations, historique. Contrôler cumul et idempotence.

## Clôtures
Répartition proportionnelle aux nuitées (intervalle [arrivée,départ[). 28 nov–5 déc, 7 000 net => 3 000 nov, 4 000 déc ; commission 600/800 à 20 %. Snapshot du taux à la confirmation via le taux effectif de CommissionRateHistory. Toute modification ultérieure des dates ou des prix conserve le taux capturé. La date de référence est la date civile de confirmation en Africa/Casablanca. Charges à date d'engagement, même impayées. Clôture mensuelle manuelle manager par bien, snapshot lignes et totaux, verrou définitif. Corrections/annulations tardives par ajustement signé dans période ouverte, lié à origine ; jamais réécrire historique. Reporter déficit de règlement au mois suivant sans répéter charges ni modifier bénéfice de la nouvelle période. Afficher prévisionnel vs clôturé. Empêcher double clôture, double allocation, double remboursement. Les revenus des séjours futurs ne sont pas acquis ; préciser traitement des séjours en cours à la date de clôture. Les frais d'annulation retenus et remboursements ajustent revenu/commission ; date de reconnaissance des frais hors nuitées à confirmer (proposition : date d'annulation, régularisation si clôturée).

## Réservations
Saisie manuelle uniquement pour Airbnb, Booking.com et Direct. Référence interne unique et référence externe facultative. Dates de séjour civiles [arrivée, départ[, départ strictement après arrivée. Vérifier les conflits avec réservations et blocages dans la même transaction. Enregistrer les paiements partiels, motifs d’annulation et remboursements. Aucun paiement en ligne. Prix direct proposé selon la priorité personnalisé > saisonnier > weekend > base, modifiable explicitement. Pour les plateformes externes, saisir les montants réels. Owner consulte ses seuls biens avec les données strictement nécessaires.

Décisions approuvées au checkpoint 6.0

1. Une réservation Pending bloque immédiatement la disponibilité.
2. En V1, un Manager autorisé fait expirer ou libère manuellement une réservation Pending. Aucun traitement automatique d’expiration n’est requis.
3. À la confirmation, capturer le taux effectif issu de CommissionRateHistory.
4. Une modification des dates ou du prix après confirmation conserve le taux d’origine.
5. Le départ et l’arrivée le même jour sont autorisés. Le nettoyage et l’état opérationnel restent indépendants de la disponibilité de réservation.
6. Rejeter la réservation si adultes et enfants dépassent la capacité maximale du bien.
7. Préserver l’historique des séjours terminés. Une correction doit suivre une procédure explicite, autorisée et auditée.
8. Une réservation NoShow peut libérer les nuits restantes uniquement sur approbation explicite d’un Manager.

Décisions complémentaires approuvées au checkpoint 6.1

- Seuls les biens Active acceptent de nouvelles réservations. Inactive et Archived les refusent. NeedsCleaning et les autres états opérationnels sont indépendants. Un blocage explicite rend des dates indisponibles.
- Fuseau métier Africa/Casablanca. Réservation le jour même et départ/arrivée le même jour permis. La saisie historique est autorisée sous réserve de conflits, d’autorisation, d’audit et de clôtures immuables. Le parcours historique complet doit être approuvé avant implémentation. Aucun horodatage de confirmation historique ne doit être inventé.
- L’expiration manuelle de Pending passe à Cancelled avec le motif PendingExpired. Conserver la réservation, la date de libération, le motif et le Manager. Aucun job d’expiration en V1.
- Seule une réservation Pending peut changer de bien. Un transfert après confirmation nécessite un parcours distinct approuvé.
- Capturer le taux effectif selon la date civile de confirmation à Casablanca. Un Pending non confirmé demeure sans taux capturé. Préserver les snapshots existants et ne pas recalculer le taux lors des changements de dates ou de prix.
- La libération des nuits non utilisées après NoShow ou départ anticipé est explicite et réservée au Manager autorisé. Préserver dates contractuelles et heures réelles, enregistrer la date effective et l’audit, contrôler les limites du séjour et l’occupation réelle. Aucune modification automatique des prix, remboursements ou commissions. Faire approuver les exceptions ambiguës avant de les traiter.
- FullCalendar Standard est approuvé pour les vues classiques et une grille Angular pour une ligne par bien. Aucun module Premium. L’interface calendrier relève du checkpoint 6.3.

Les exceptions restantes et les limites des workflows sont détaillées dans [les règles de réservation](docs/RESERVATION_BUSINESS_RULES.md).

Décisions approuvées au checkpoint 6.2

1. Parcours distinct pour un séjour historique, avec dates réelles, justificatif des montants et audit complet.
2. Pour ce parcours, rechercher le taux à la date réelle de confirmation saisie par le Manager. Aucune date de substitution.
3. Créer en Pending ou directement en Confirmed via la même logique de confirmation, au sein d'une transaction.
4. Proposer les prix par nuit des réservations Direct via le tarif du bien. Autoriser une substitution explicite.
5. Saisir les montants réels Airbnb ou Booking.com. Conserver la distinction entre nuits saisies et nuits réparties.
6. Conserver les prix des nuits inchangées, proposer les nouvelles nuits et laisser les anciennes révisions consultables.
7. Suggérer les voyageurs par e-mail ou téléphone normalisés. Le Manager choisit. Aucun rapprochement automatique.
8. Exiger l'unicité de la référence externe facultative par organisation et source dans SQL Server.
9. Réserver les extensions et départs anticipés CheckedIn aux actions dédiées du checkpoint 6.4.
10. Accepter le brut moins les frais ou le net direct. Conserver les montants inconnus à null.

La répartition des centimes résiduels attribue un centime à chaque nuit non renseignée, de la plus ancienne à la plus récente, après division en centimes entiers. Ce calcul décrit le prix accepté. Le montant de commission de réservation est arrondi au centime, à mi-distance en s'éloignant de zéro. Les règles d'allocation comptable restent au checkpoint financier. En historique, accepter uniquement les séjours terminés sur un bien Active, selon sa capacité actuelle, avec date réelle de confirmation et PDF privé conforme à la politique d'analyse. Une date connue sans heure reste une date.

## Calendrier et blocages de dates (6.3)

1. Le calendrier Manager s’ouvre sur une grille de 30 jours consécutifs, avec une ligne par bien et la date métier Africa/Casablanca comme point de départ.
2. Sélectionner une période disponible ouvre le formulaire de réservation existant avec le bien et les dates préremplis. La création exige la validation explicite du formulaire.
3. Le glisser-déposer et le redimensionnement des réservations sont désactivés. Toute modification passe par le formulaire de révision autorisé.
4. Les motifs de blocage manuel sont `Maintenance`, `OwnerUse`, `Unavailability`, `Renovation` et `Other`, traduits en français, anglais et arabe.
5. Les conflits entre réservations et blocages et entre blocages restent stricts sous le verrou SQL partagé. Aucun forçage.
6. Les blocages peuvent être modifiés dans les limites autorisées et annulés avec audit. Le dossier historique est conservé sans suppression physique.
7. Un blocage exige une date de début et une date de fin exclusive. Une prolongation future passe par la modification autorisée.
8. La couleur indique la source de réservation. Le statut et le motif de blocage disposent de libellés ou d’indicateurs accessibles distincts.
9. Les réservations annulées sont masquées par défaut et affichables sur demande. Le filtre ne modifie ni la disponibilité ni la protection des nuits historiques.
10. Le serveur calcule les arrivées prévues, les départs prévus et les nombres de biens occupés, disponibles et bloqués à la date sélectionnée.
11. La création rétroactive de blocages est interdite selon la date métier Africa/Casablanca. Les blocages historiques existants sont conservés.
12. Chaque blocage concerne exactement un bien. La création groupée est hors périmètre V1.

Précisions approuvées pour 6.3. Les biens occupés comptent les nuits Confirmed ou CheckedIn et les nuits historiques effectivement occupées. Les nouveaux blocages sont admis sur les biens Active ou Inactive. Un changement ou une annulation de blocage en cours conserve les nuits écoulées et peut libérer des dates à partir d’aujourd’hui, heure métier Africa/Casablanca.

## Charges et opérations
Catégories personnalisables (eau, électricité, internet, nettoyage, linge, maintenance, mobilier, consommables, syndic, assurance, taxes, autres). Charge récurrente créée automatiquement en Draft, confirmation manager avec justificatif obligatoire. Paiements partiels, échéances, alertes dues ; une charge par bien. Pas de fournisseur dédié, charge commune ou dépense interne. Annulation/ajustement tracés. Nettoyage auto après checkout confirmé, unique par réservation ; nettoyage manuel possible. Checklist snapshot du modèle ; intervenants nom/tel/spécialité sans login. Maintenance simple, tâches manuelles/auto liste/Kanban/calendrier ; photos optionnelles, charge liée créée après confirmation avec justificatif. Manager décide blocage. Statut opérationnel indépendant du statut réservation.

## Demandes et KPI
Owner crée demandes maintenance, justificatif, versement, blocage, modification bien, autre ; messages et historique ; manager traite et approuve/rejette, applique action explicitement. Notifications internes + email sans détails sensibles. Occupation = nuits occupées / nuits commercialisables hors blocages ; si dénominateur 0, N/A. Distinguer occupation réalisée et réservée ; pas ADR/RevPAR en V1. Agréger ratio par somme numérateurs/dénominateurs, pas moyenne brute. Filtres jour/semaine/mois/trimestre/année/personnalisé, bien/owner/source selon droits ; comparaisons période précédente/année précédente/biens/sources. Rapports manuels PDF/Excel : mensuel, propriétaire, réservations, charges, occupation, commissions, annuel. Afficher données inconnues comme telles, pas zéro inventé.

## Tests d'acceptation
Tester tous exemples ci-dessus, taux historique, net direct, arrondis, cross-month, encaissements mixtes, impayés, remboursements partiels, déficit reporté, clôture immuable, concurrence réservations et versements, absence de duplication jobs, isolation owner/tenant, exports identiques aux données autorisées. Documenter les hypothèses non validées avant usage réel.
