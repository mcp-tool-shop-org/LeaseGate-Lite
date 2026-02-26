<p align="center">
  <a href="README.ja.md">日本語</a> | <a href="README.md">English</a> | <a href="README.es.md">Español</a> | <a href="README.fr.md">Français</a> | <a href="README.hi.md">हिन्दी</a> | <a href="README.it.md">Italiano</a> | <a href="README.pt-BR.md">Português (BR)</a>
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

一个单标签的 MAUI 控制界面和一个本地守护进程，用于在 Windows 上限制 AI 任务的负载，从而实现更流畅的操作，减少卡顿，并减少过热现象。

它保留了 LeaseGate 的核心功能（显式控制、受限执行、可预测的原因、可观察的状态），但将其范围缩小到家用 PC。

## 项目

| 项目 | 描述 |
| --------- | ------------- |
| `src/LeaseGateLite.Contracts` | 共享的 DTO 和枚举 (net9.0 + net10.0) |
| `src/LeaseGateLite.Daemon` | 本地 API 守护进程，位于 `localhost:5177`，提供真实的 Windows 系统指标。 |
| `src/LeaseGateLite.App` | 一个单标签的 MAUI 控制面板（Windows/Android/iOS/macCatalyst）。 |
| `src/LeaseGateLite.Tray` | Windows 系统托盘助手。 |
| `tests/LeaseGateLite.Tests` | 178 个 xUnit 测试（配置验证、模拟、诊断）。 |

## 运行

1) 启动守护进程：

```powershell
dotnet run --project src/LeaseGateLite.Daemon
```

2) 启动 MAUI 应用程序（Windows）：

```powershell
dotnet build src/LeaseGateLite.App -f net10.0-windows10.0.19041.0
dotnet run --project src/LeaseGateLite.App -f net10.0-windows10.0.19041.0
```

## 一键打包和安装（Windows）

创建发布包（可移植的 zip 文件 + SHA256 校验和）：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package-v0.1.0.ps1
```

从打包的包中本地安装：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/install-local.ps1 -EnableAutostart
```

安装后的行为：
- 守护进程立即运行，并且可以配置为在登录时自动启动。
- 控制面板自动启动并连接。
- 默认配置为“平衡”模式；对于笔记本电脑，首次启动时会推荐“静音”模式（但不会强制）。

## 守护进程的端点

| 方法 | 路径 | 描述 |
| -------- | ------ | ------------- |
| `GET` | `/status` | 实时 `StatusSnapshot`（CPU%，RAM%，队列深度，温度状态）。 |
| `GET` | `/config` | 当前配置。 |
| `POST` | `/config` | 应用配置。 |
| `POST` | `/config/reset` | 重置为默认值。 |
| `POST` | `/service/start` | 启动守护进程。 |
| `POST` | `/service/stop` | 停止守护进程。 |
| `POST` | `/service/restart` | 重启守护进程。 |
| `POST` | `/diagnostics/export` | 导出 JSON 诊断包。 |
| `GET` | `/events/tail?n=200` | 事件日志。 |

## 单标签布局

单标签/页面：**控制**

- **顶部栏：** 状态指示点、模式选择器、端点、快速操作（启动、停止、应用、导出诊断）。
- **左侧列：** 可审计的检查清单（可跳转到卡片）。
- **右侧列：** 按照检查清单部分的顺序排列的控制卡片。

每个卡片包含：当前值、简短说明、控件、效果预览以及覆盖范围说明。

## 审计检查清单

| 部分 | 控件 |
| --------- | ---------- |
| **A) Service** | 连接、启动/停止/重启、版本 + 运行时间、配置位置、重置。 |
| **B) Live status** | 温度状态（平静/温暖/过热）、活动调用数、队列深度、CPU%，RAM%。 |
| **C) Core throttling** | 最大并发数、交互式保留、后台限制、冷却时间。 |
| **D) Adaptive tuning** | 软/硬限制、恢复速率、平滑度。 |
| **E) Request shaping** | 最大输出/提示限制、溢出行为、重试策略。 |
| **F) Rate limiting** | 每分钟请求数、每分钟令牌数、突发量。 |
| **G) Presets** | 安静（笔记本电脑）、平衡、性能（台式机）。 |
| **H) Diagnostics** | 导出诊断信息、查看事件日志、复制状态摘要。 |

## 备注

- Lite 版本有意省略了高级的治理功能（审批、签名、凭证）。
- 守护进程读取真实的 Windows 系统指标（通过 PerformanceCounter 获取 CPU 使用率，通过 GlobalMemoryStatusEx 获取 RAM 使用率），并模拟队列压力动态。
- 测试使用 `FakeSystemMetrics` 提供程序，通过依赖注入进行确定性的、与硬件无关的验证。

---

<p align="center">
  Built by <a href="https://mcp-tool-shop.github.io/">MCP Tool Shop</a>
</p>
