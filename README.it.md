<p align="center">
  <a href="README.ja.md">日本語</a> | <a href="README.zh.md">中文</a> | <a href="README.es.md">Español</a> | <a href="README.fr.md">Français</a> | <a href="README.hi.md">हिन्दी</a> | <a href="README.md">English</a> | <a href="README.pt-BR.md">Português (BR)</a>
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

Un'interfaccia di controllo MAUI con una sola scheda e un demone locale per limitare i carichi di lavoro dell'intelligenza artificiale su Windows: prestazioni più fluide, meno rallentamenti, meno picchi termici.

Mantiene la sensazione operativa di LeaseGate (controllo esplicito, esecuzione limitata, motivazioni deterministiche, stato osservabile), ma è ottimizzata per l'uso su PC domestici.

## Progetti

| Progetto | Descrizione |
| --------- | ------------- |
| `src/LeaseGateLite.Contracts` | DTO e enumerazioni condivise (net9.0 + net10.0) |
| `src/LeaseGateLite.Daemon` | Demone API locale su `localhost:5177` con metriche reali del sistema Windows. |
| `src/LeaseGateLite.App` | Pannello di controllo MAUI con una sola scheda (Windows/Android/iOS/macCatalyst). |
| `src/LeaseGateLite.Tray` | Componente per la barra delle applicazioni di Windows. |
| `tests/LeaseGateLite.Tests` | 178 test xUnit (validazione della configurazione, simulazione, diagnostica). |

## Esecuzione

1) Avvia il demone:

```powershell
dotnet run --project src/LeaseGateLite.Daemon
```

2) Avvia l'app MAUI (Windows):

```powershell
dotnet build src/LeaseGateLite.App -f net10.0-windows10.0.19041.0
dotnet run --project src/LeaseGateLite.App -f net10.0-windows10.0.19041.0
```

## Pacchettizzazione e installazione con un solo clic (Windows)

Crea un artefatto di rilascio (file zip portatile + checksum SHA256):

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package-v0.1.0.ps1
```

Installa localmente dall'artefatto pacchettizzato:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/install-local.ps1 -EnableAutostart
```

Comportamento post-installazione:
- Il demone viene avviato immediatamente e può essere configurato per l'avvio all'accesso.
- Il pannello di controllo viene avviato e si connette automaticamente.
- "Bilanciato" è la configurazione predefinita; i dispositivi con hardware simile a un laptop ricevono la raccomandazione "Silenzioso" durante la configurazione iniziale (non forzata).

## Endpoint del demone

| Metodo | Percorso | Descrizione |
| -------- | ------ | ------------- |
| `GET` | `/status` | `StatusSnapshot` in tempo reale (CPU%, RAM%, profondità della coda, stato termico). |
| `GET` | `/config` | Configurazione corrente. |
| `POST` | `/config` | Applica la configurazione. |
| `POST` | `/config/reset` | Ripristina le impostazioni predefinite. |
| `POST` | `/service/start` | Avvia il demone. |
| `POST` | `/service/stop` | Arresta il demone. |
| `POST` | `/service/restart` | Riavvia il demone. |
| `POST` | `/diagnostics/export` | Esporta il pacchetto di diagnostica in formato JSON. |
| `GET` | `/events/tail?n=200` | Visualizzazione degli eventi. |

## Layout con una sola scheda

Layout con una sola scheda/pagina: **Controllo**

- **Barra superiore**: indicatore di stato, selettore della modalità, endpoint, azioni rapide (Avvia, Arresta, Applica, Esporta diagnostica).
- **Colonna sinistra**: elenco di controllo verificabile (passa alla scheda corrispondente).
- **Colonna destra**: schede di controllo ordinate in base alle sezioni dell'elenco di controllo.

Ogni scheda include: valore corrente, breve descrizione, controlli, anteprima dell'effetto e una nota esplicativa.

## Elenco di controllo per la verifica

| Sezione | Controlli |
| --------- | ---------- |
| **A) Service** | Connessione, Avvia/Arresta/Riavvia, versione + tempo di attività, posizione della configurazione, ripristino. |
| **B) Live status** | Stato termico (Calmo/Tiepido/Caldo), chiamate attive, profondità della coda, CPU%, RAM%. |
| **C) Core throttling** | Concurrency massimo, riserva interattiva, limite in background, cooldown. |
| **D) Adaptive tuning** | Soglie morbide/rigide, velocità di ripristino, smoothing. |
| **E) Request shaping** | Limite massimo di output/prompt, comportamento in caso di overflow, politica di retry. |
| **F) Rate limiting** | Richieste/min, token/min, tolleranza per i picchi. |
| **G) Presets** | Silenzioso (laptop), Bilanciato, Prestazioni (desktop). |
| **H) Diagnostics** | Esporta diagnostica, visualizzazione degli eventi, copia del riepilogo dello stato. |

## Note

- La versione "Lite" esclude intenzionalmente funzionalità di governance avanzate (approvazioni, firma, ricevute).
- Il demone legge metriche reali del sistema Windows (CPU tramite PerformanceCounter, RAM tramite GlobalMemoryStatusEx) e simula la pressione sulla coda.
- I test utilizzano un provider `FakeSystemMetrics` tramite dependency injection per la verifica deterministica e indipendente dall'hardware.

---

<p align="center">
  Built by <a href="https://mcp-tool-shop.github.io/">MCP Tool Shop</a>
</p>
