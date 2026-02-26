<p align="center">
  <a href="README.ja.md">日本語</a> | <a href="README.zh.md">中文</a> | <a href="README.es.md">Español</a> | <a href="README.fr.md">Français</a> | <a href="README.hi.md">हिन्दी</a> | <a href="README.it.md">Italiano</a> | <a href="README.md">English</a>
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

Uma interface de controle MAUI em uma única aba e um daemon local para limitar a carga de trabalho de IA no Windows – chamadas mais suaves, menos travamentos, menos picos de temperatura.

Mantém a sensação operacional do LeaseGate (controle explícito, execução limitada, motivos determinísticos, status observável), mas é adaptado para uso em computadores domésticos.

## Projetos

| Projeto | Descrição |
| --------- | ------------- |
| `src/LeaseGateLite.Contracts` | Objetos de transferência de dados (DTOs) e enumerações compartilhados (net9.0 + net10.0) |
| `src/LeaseGateLite.Daemon` | Daemon da API local em `localhost:5177` com métricas reais do sistema Windows. |
| `src/LeaseGateLite.App` | Painel de controle MAUI em uma única aba (Windows/Android/iOS/macCatalyst). |
| `src/LeaseGateLite.Tray` | Aplicativo complementar para a bandeja do sistema Windows. |
| `tests/LeaseGateLite.Tests` | 178 testes Unit (validação de configuração, simulação, diagnóstico). |

## Executar

1) Iniciar o daemon:

```powershell
dotnet run --project src/LeaseGateLite.Daemon
```

2) Iniciar o aplicativo MAUI (Windows):

```powershell
dotnet build src/LeaseGateLite.App -f net10.0-windows10.0.19041.0
dotnet run --project src/LeaseGateLite.App -f net10.0-windows10.0.19041.0
```

## Empacotamento e instalação com um clique (Windows)

Criar um artefato de lançamento (arquivo zip portátil + checksum SHA256):

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package-v0.1.0.ps1
```

Instalar localmente a partir do artefato empacotado:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/install-local.ps1 -EnableAutostart
```

Comportamento pós-instalação:
- O daemon é iniciado imediatamente e pode ser configurado para iniciar na inicialização.
- O painel de controle é iniciado e se conecta automaticamente.
- "Equilibrado" é a configuração padrão; dispositivos semelhantes a laptops recebem uma recomendação "Silencioso" na configuração inicial (nunca forçada).

## Endpoints do daemon

| Método | Caminho | Descrição |
| -------- | ------ | ------------- |
| `GET` | `/status` | `StatusSnapshot` em tempo real (CPU%, RAM%, profundidade da fila, estado de temperatura) |
| `GET` | `/config` | Configuração atual |
| `POST` | `/config` | Aplicar configuração |
| `POST` | `/config/reset` | Restaurar padrões |
| `POST` | `/service/start` | Iniciar daemon |
| `POST` | `/service/stop` | Parar daemon |
| `POST` | `/service/restart` | Reiniciar daemon |
| `POST` | `/diagnostics/export` | Exportar pacote de diagnóstico em JSON |
| `GET` | `/events/tail?n=200` | Visualização de eventos |

## Layout em uma única aba

Layout de uma única aba/página: **Controle**

- **Barra de cabeçalho**: ponto de status, seletor de modo, endpoint, ações rápidas (Iniciar, Parar, Aplicar, Exportar Diag)
- **Coluna esquerda**: lista de verificação auditável (ir para o cartão)
- **Coluna direita**: cartões de controle ordenados, correspondendo às seções da lista de verificação

Cada cartão inclui: valor atual, significado breve, controles, visualização do efeito e um rodapé de cobertura.

## Lista de verificação de auditoria

| Seção | Controles |
| --------- | ---------- |
| **A) Service** | Conectar, Iniciar/Parar/Reiniciar, versão + tempo de atividade, localização da configuração, restaurar padrões |
| **B) Live status** | Estado de temperatura (Calmo/Quente/Quente), chamadas ativas, profundidade da fila, CPU%, RAM% |
| **C) Core throttling** | Concorrência máxima, reserva interativa, limite de segundo plano, tempo de recuperação |
| **D) Adaptive tuning** | Limites suaves/rígidos, taxa de recuperação, suavização |
| **E) Request shaping** | Limite máximo de saída/prompt, comportamento de estouro, política de repetição |
| **F) Rate limiting** | Solicitações/minuto, tokens/minuto, tolerância de explosão |
| **G) Presets** | Silencioso (laptop), Equilibrado, Desempenho (desktop) |
| **H) Diagnostics** | Exportar diagnósticos, visualização de eventos, copiar resumo de status |

## Notas

- A versão "Lite" exclui intencionalmente recursos pesados de governança (aprovações, assinatura, recibos).
- O daemon lê métricas reais do sistema Windows (CPU via PerformanceCounter, RAM via GlobalMemoryStatusEx) e simula a dinâmica da pressão da fila.
- Os testes usam um provedor `FakeSystemMetrics` via injeção de dependência para verificação determinística e independente do hardware.

---

<p align="center">
  Built by <a href="https://mcp-tool-shop.github.io/">MCP Tool Shop</a>
</p>
