<p align="center">
  <a href="README.ja.md">日本語</a> | <a href="README.zh.md">中文</a> | <a href="README.md">English</a> | <a href="README.fr.md">Français</a> | <a href="README.hi.md">हिन्दी</a> | <a href="README.it.md">Italiano</a> | <a href="README.pt-BR.md">Português (BR)</a>
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

Una interfaz de control MAUI de una sola pestaña y un demonio local para limitar la carga de trabajo de la IA en Windows: llamadas más fluidas, menos tartamudeo, menos picos de temperatura.

Mantiene la sensación operativa de LeaseGate (control explícito, ejecución limitada, razones deterministas, estado observable), pero se adapta al ámbito de un PC doméstico.

## Proyectos

| Proyecto | Descripción |
| --------- | ------------- |
| `src/LeaseGateLite.Contracts` | DTOs y enumeraciones compartidos (net9.0 + net10.0) |
| `src/LeaseGateLite.Daemon` | Demonio de API local en `localhost:5177` con métricas reales del sistema Windows. |
| `src/LeaseGateLite.App` | Panel de control MAUI de una sola pestaña (Windows/Android/iOS/macCatalyst). |
| `src/LeaseGateLite.Tray` | Aplicación complementaria para la bandeja del sistema de Windows. |
| `tests/LeaseGateLite.Tests` | 178 pruebas de unidad (validación de configuración, simulación, diagnóstico). |

## Ejecutar

1) Iniciar demonio:

```powershell
dotnet run --project src/LeaseGateLite.Daemon
```

2) Iniciar aplicación MAUI (Windows):

```powershell
dotnet build src/LeaseGateLite.App -f net10.0-windows10.0.19041.0
dotnet run --project src/LeaseGateLite.App -f net10.0-windows10.0.19041.0
```

## Empaquetado e instalación con un solo clic (Windows)

Crear artefacto de lanzamiento (zip portátil + suma de comprobación SHA256):

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package-v0.1.0.ps1
```

Instalar localmente desde el artefacto empaquetado:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/install-local.ps1 -EnableAutostart
```

Comportamiento después de la instalación:
- El demonio se ejecuta inmediatamente y se puede configurar para que se inicie al iniciar sesión.
- El panel de control se inicia y se conecta automáticamente.
- "Equilibrado" es la configuración predeterminada; el hardware similar al de una computadora portátil recibe una recomendación "Silencioso" durante la configuración inicial (nunca se fuerza).

## Puntos finales del demonio

| Método | Ruta | Descripción |
| -------- | ------ | ------------- |
| `GET` | `/status` | `StatusSnapshot` en vivo (%, RAM%, profundidad de la cola, estado de temperatura) |
| `GET` | `/config` | Configuración actual |
| `POST` | `/config` | Aplicar configuración |
| `POST` | `/config/reset` | Restablecer valores predeterminados |
| `POST` | `/service/start` | Iniciar demonio |
| `POST` | `/service/stop` | Detener demonio |
| `POST` | `/service/restart` | Reiniciar demonio |
| `POST` | `/diagnostics/export` | Exportar paquete de diagnóstico JSON |
| `GET` | `/events/tail?n=200` | Cola de eventos |

## Diseño de una sola pestaña

Diseño de una sola pestaña/página: **Control**

- **Barra de encabezado**: punto de estado, selector de modo, punto final, acciones rápidas (Iniciar, Detener, Aplicar, Exportar diagnóstico)
- **Columna izquierda**: lista de verificación auditable (ir a la tarjeta)
- **Columna derecha**: tarjetas de control ordenadas que coinciden con las secciones de la lista de verificación

Cada tarjeta incluye: valor actual, significado breve, controles, vista previa del efecto y un pie de página de cobertura.

## Lista de verificación de auditoría

| Sección | Controles |
| --------- | ---------- |
| **A) Service** | Conectar, Iniciar/Detener/Reiniciar, versión + tiempo de actividad, ubicación de la configuración, restablecer |
| **B) Live status** | Estado de temperatura (Calm/Warm/Spicy), llamadas activas, profundidad de la cola, %, RAM% |
| **C) Core throttling** | Concurrencia máxima, reserva interactiva, límite de segundo plano, tiempo de enfriamiento |
| **D) Adaptive tuning** | Umbrales suaves/duros, tasa de recuperación, suavizado |
| **E) Request shaping** | Límite máximo de salida/prompt, comportamiento de desbordamiento, política de reintento |
| **F) Rate limiting** | Solicitudes/min, tokens/min, límite de ráfaga |
| **G) Presets** | Silencioso (portátil), Equilibrado, Rendimiento (escritorio) |
| **H) Diagnostics** | Exportar diagnósticos, cola de eventos, copiar resumen de estado |

## Notas

- Lite excluye intencionalmente funciones de gobernanza complejas (aprobaciones, firma, recibos).
- El demonio lee métricas reales del sistema Windows (CPU a través de PerformanceCounter, RAM a través de GlobalMemoryStatusEx) y simula la dinámica de la presión de la cola.
- Las pruebas utilizan un proveedor `FakeSystemMetrics` a través de la inyección de dependencias para la verificación determinista e independiente del hardware.

---

<p align="center">
  Built by <a href="https://mcp-tool-shop.github.io/">MCP Tool Shop</a>
</p>
