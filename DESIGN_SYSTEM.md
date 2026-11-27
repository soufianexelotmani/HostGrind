# HostGrid — Design System

Style premium immobilier (luxury), sobre, chaleureux, pas de surcharge décorative. Palette Terracotta : primary #B45338, accent #E8C49C, background #FAF7F2, text #30251F. Créer tokens CSS sémantiques light/dark/system, surfaces, bordures, focus, success/warning/error ; vérifier contrastes WCAG AA, ne pas utiliser uniquement la couleur pour statuts. Manrope latin + Tajawal arabe (polices licenciées, fallback système). Lucide icons, transitions légères, respecter prefers-reduced-motion. Branding personnalisable SuperAdmin (nom/logo/identité), logo provisoire ; aucune marque commerciale validée.

## Layout
Desktop sidebar rétractable, header avec organisation autorisée, recherche globale Ctrl+K, notifications, profil, langue et thème. Mobile bottom navigation 4–5 destinations + menu complémentaire ; tablette adaptative. Login split illustration immobilière originale/licenciée et formulaire ; MFA/reset/activation. RTL complet via dir/lang, propriétés CSS logiques, navigation et composants testés en arabe. Dates/montants localisés MAD. Aucun texte UI hardcodé hors dictionnaires FR/EN/AR.

## Navigation
Manager : Dashboard, Biens, Propriétaires, Réservations, Calendrier, Finances, Charges, Versements, Nettoyage, Maintenance, Tâches, Demandes, Rapports, Paramètres. Owner : Dashboard, Mes biens, Réservations, Calendrier, Mes revenus, Mes charges, Mes versements, Interventions, Mes documents, Mes demandes, Mes rapports, Mon profil. Admin : dashboard global, organisations, utilisateurs/managers/owners, biens, finances globales, audit, santé, paramètres, rapports. Menu filtré par droits, sans substituer autorisation serveur.

## Composants et pages
PrimeNG + Tailwind, composants partagés PageHeader, KPI card, status chip, filter bar, data table, date range, money input, file upload, confirm dialog, empty/error/loading skeleton, toast, pagination, export button. Tables avancées : recherche, tri, filtres, pagination serveur, colonnes personnalisables, exports autorisés ; mobile cartes ou scroll accessible. Formulaires réactifs, erreurs inline traduites, focus clavier, labels, confirmations actions sensibles. Calendrier FullCalendar standard jour/semaine/mois, global/par bien ; ne pas dépendre de resource timeline premium, utiliser filtre bien et vue agrégée. Tâches Kanban + liste + calendrier.

## Dashboards
Manager : nombre biens/owners/réservations, occupation, net, commissions, charges, bénéfices, arrivées/départs, factures dues, demandes, versements, maintenance. Owner : mêmes KPI propres, paiements reçus/restants, calendrier, documents, interventions. Admin : volumes globaux, finance, audit, API/DB/storage/jobs santé réelle. ECharts interactifs (tooltip, filtres, drilldown autorisé), widgets réordonnables et préférences persistées, actualisation après mutation + polling raisonnable. Aucun chiffre fictif en production. Rapports PDF/Excel cohérents avec filtres et branding, période/référence/date, police arabe et RTL PDF vérifiés ; ne pas promettre export arabe avant test.

## PWA
Installable, manifest/icons, service worker app shell seulement ; online-only, écran hors ligne explicite, aucune mise en cache API, documents, tokens ou données personnelles. Vérifier déconnexion et changement de compte. Pas de synchronisation offline.
