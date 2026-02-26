<p align="center">
  <a href="README.ja.md">日本語</a> | <a href="README.zh.md">中文</a> | <a href="README.es.md">Español</a> | <a href="README.md">English</a> | <a href="README.hi.md">हिन्दी</a> | <a href="README.it.md">Italiano</a> | <a href="README.pt-BR.md">Português (BR)</a>
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/mcp-tool-shop-org/brand/main/logos/LeaseGate-Lite/readme.jpg" width="500">
</p>

<p align="center">
  <a href="https://github.com/mcp-tool-shop-org/LeaseGate-Lite/actions/workflows/ci.yml"><img src="https://github.com/mcp-tool-shop-org/LeaseGate-Lite/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-10.0-512BD4" alt=".NET 10"></a>
  <a href="LICENSE"><img src="https://img.shields.io/github/license/mcp-tool-shop-org/LeaseGate-Lite" alt="License: MIT"></a>
  <a href="https://mcp-tool-shop-org.github.io/LeaseGate-Lite/"><img src="https://img.shields.io/badge/Landing_Page-live-blue" alt="Landing Page"></a>
</p>

Une interface de contrôle MAUI en une seule fenêtre et un démon local pour limiter la charge des tâches d'IA sur Windows : performances améliorées, moins de saccades, moins de pics thermiques.

Maintient l'aspect opérationnel de LeaseGate (contrôle explicite, exécution limitée, raisons déterminées, statut observable) mais est adapté à un usage sur PC personnel.

## Projets

| Projet | Description |
| --------- | ------------- |
| `src/LeaseGateLite.Contracts` | Objets de données partagés (DTO) et énumérations (net9.0 + net10.0) |
| `src/LeaseGateLite.Daemon` | Démon API local sur `localhost:5177` avec des métriques réelles du système Windows. |
| `src/LeaseGateLite.App` | Panneau de contrôle MAUI en une seule fenêtre (Windows/Android/iOS/macCatalyst). |
| `src/LeaseGateLite.Tray` | Application compagnon pour la barre des tâches Windows. |
| `tests/LeaseGateLite.Tests` | 178 tests unitaires (validation de la configuration, simulation, diagnostics). |

## Exécution

1) Démarrer le démon :

```powershell
dotnet run --project src/LeaseGateLite.Daemon
```

2) Démarrer l'application MAUI (Windows) :

```powershell
dotnet build src/LeaseGateLite.App -f net10.0-windows10.0.19041.0
dotnet run --project src/LeaseGateLite.App -f net10.0-windows10.0.19041.0
```

## Emballage et installation en un clic (Windows)

Créer un artefact de publication (fichier ZIP portable + checksum SHA256) :

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package-v0.1.0.ps1
```

Installer localement à partir de l'artefact emballé :

```powershell
powershell -ExecutionPolicy Bypass -File scripts/install-local.ps1 -EnableAutostart
```

Comportement après l'installation :
- Le démon démarre immédiatement et peut être configuré pour démarrer au démarrage de la session.
- Le panneau de contrôle se lance et se connecte automatiquement.
- "Équilibré" est le paramètre par défaut ; les ordinateurs portables reçoivent une recommandation "Silencieux" lors de la configuration initiale (ce qui n'est jamais imposé).

## Points d'accès du démon

| Méthode | Chemin | Description |
| -------- | ------ | ------------- |
| `GET` | `/status` | `StatusSnapshot` en direct (%, RAM %, profondeur de la file d'attente, état thermique) |
| `GET` | `/config` | Configuration actuelle |
| `POST` | `/config` | Appliquer la configuration |
| `POST` | `/config/reset` | Réinitialiser les paramètres par défaut |
| `POST` | `/service/start` | Démarrer le démon |
| `POST` | `/service/stop` | Arrêter le démon |
| `POST` | `/service/restart` | Redémarrer le démon |
| `POST` | `/diagnostics/export` | Exporter le bundle de diagnostics au format JSON |
| `GET` | `/events/tail?n=200` | Affichage des événements |

## Disposition en une seule fenêtre

Disposition unique : **Contrôle**

- **Barre d'en-tête** : point d'état, sélecteur de mode, point d'accès, actions rapides (Démarrer, Arrêter, Appliquer, Exporter les diagnostics).
- **Colonne de gauche** : liste de contrôle vérifiable (lien vers la carte).
- **Colonne de droite** : cartes de contrôle ordonnées correspondant aux sections de la liste de contrôle.

Chaque carte comprend : la valeur actuelle, une brève explication, les contrôles, un aperçu de l'effet et un pied de page indiquant la couverture.

## Liste de contrôle

| Section | Contrôles |
| --------- | ---------- |
| **A) Service** | Connexion, Démarrer/Arrêter/Redémarrer, version + durée de fonctionnement, emplacement de la configuration, réinitialisation. |
| **B) Live status** | État thermique (Calme/Chaud/Fervent), nombre d'appels actifs, profondeur de la file d'attente, %, RAM %. |
| **C) Core throttling** | Concurrence maximale, réserve interactive, limite de fond, période de refroidissement. |
| **D) Adaptive tuning** | Seuils (mou/dur), taux de récupération, lissage. |
| **E) Request shaping** | Limite de sortie/de requête maximale, comportement en cas de débordement, politique de nouvelle tentative. |
| **F) Rate limiting** | Requêtes/minute, jetons/minute, marge de manœuvre pour les pics. |
| **G) Presets** | Silencieux (ordinateur portable), Équilibré, Performance (ordinateur de bureau). |
| **H) Diagnostics** | Exporter les diagnostics, affichage des événements, copier le résumé de l'état. |

## Notes

- La version "Lite" exclut intentionnellement les fonctionnalités de gouvernance avancées (approbations, signature, reçus).
- Le démon lit les métriques réelles du système Windows (CPU via PerformanceCounter, RAM via GlobalMemoryStatusEx) et simule la pression de la file d'attente.
- Les tests utilisent un fournisseur `FakeSystemMetrics` via l'injection de dépendances pour une vérification déterministe et indépendante du matériel.

---

<p align="center">
  Built by <a href="https://mcp-tool-shop.github.io/">MCP Tool Shop</a>
</p>
