<p align="center">
  <a href="README.md">English</a> | <a href="README.zh.md">中文</a> | <a href="README.es.md">Español</a> | <a href="README.fr.md">Français</a> | <a href="README.hi.md">हिन्दी</a> | <a href="README.it.md">Italiano</a> | <a href="README.pt-BR.md">Português (BR)</a>
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

Windows上でAIワークロードを制御するための、MAUIコントロールパネルとローカルデーモン。よりスムーズな動作、スタッターの軽減、サーマルスパイクトの抑制を実現します。

LeaseGateの操作感（明示的な制御、実行範囲の制限、決定的な理由、監視可能な状態）を維持しつつ、ホームPCでの利用に最適化されています。

## プロジェクト

| プロジェクト | 説明 |
| --------- | ------------- |
| `src/LeaseGateLite.Contracts` | 共有DTOと列挙型 (net9.0 + net10.0) |
| `src/LeaseGateLite.Daemon` | `localhost:5177`で動作するローカルAPIデーモン。実際のWindowsシステムメトリクスを使用。 |
| `src/LeaseGateLite.App` | MAUIコントロールパネル（Windows/Android/iOS/macCatalyst対応、1タブ表示） |
| `src/LeaseGateLite.Tray` | Windowsシステムトレイ用コンパニオンアプリ |
| `tests/LeaseGateLite.Tests` | 178個のxUnitテスト（設定検証、シミュレーション、診断） |

## 実行

1) デーモンの起動:

```powershell
dotnet run --project src/LeaseGateLite.Daemon
```

2) MAUIアプリの起動（Windows）:

```powershell
dotnet build src/LeaseGateLite.App -f net10.0-windows10.0.19041.0
dotnet run --project src/LeaseGateLite.App -f net10.0-windows10.0.19041.0
```

## ワンクリックでパッケージングとインストール（Windows）

リリースアーティファクトの作成（ポータブルなzipファイル + SHA256チェックサム）:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package-v0.1.0.ps1
```

パッケージ化されたアーティファクトからのローカルインストール:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/install-local.ps1 -EnableAutostart
```

インストール後の動作:
- デーモンは起動後すぐに動作し、ログイン時に自動起動するように設定できます。
- コントロールパネルは自動的に起動し、接続されます。
- デフォルトは「バランス」モードです。ノートパソコンのようなハードウェアでは、初回設定時に「静音」モードが推奨されます（強制ではありません）。

## デーモンのエンドポイント

| メソッド | パス | 説明 |
| -------- | ------ | ------------- |
| `GET` | `/status` | CPU使用率、メモリ使用率、キューの深さ、温度状態などのリアルタイム情報 (`StatusSnapshot`) |
| `GET` | `/config` | 現在の設定 |
| `POST` | `/config` | 設定の適用 |
| `POST` | `/config/reset` | デフォルトへのリセット |
| `POST` | `/service/start` | デーモンの起動 |
| `POST` | `/service/stop` | デーモンの停止 |
| `POST` | `/service/restart` | デーモンの再起動 |
| `POST` | `/diagnostics/export` | 診断情報のJSONエクスポート |
| `GET` | `/events/tail?n=200` | イベントログの表示 |

## 1タブ表示

1つのタブ/ページ: **制御**

- **ヘッダー**: ステータス表示、モード選択、エンドポイント、クイックアクション（開始、停止、適用、診断エクスポート）
- **左側の列**: 監査可能なチェックリスト（カードへのジャンプ）
- **右側の列**: チェックリストのセクションに対応する制御カードが順番に表示

各カードには、現在の値、簡単な説明、制御、効果のプレビュー、および範囲に関する注釈が含まれます。

## 監査チェックリスト

| セクション | 制御項目 |
| --------- | ---------- |
| **A) Service** | 接続、開始/停止/再起動、バージョンと稼働時間、設定ファイルの場所、リセット |
| **B) Live status** | 温度状態（通常/注意/高温）、アクティブな処理数、キューの深さ、CPU使用率、メモリ使用率 |
| **C) Core throttling** | 最大同時実行数、インタラクティブ処理の優先度、バックグラウンド処理の制限、クールダウン時間 |
| **D) Adaptive tuning** | ソフト/ハード制限、復元速度、平滑化 |
| **E) Request shaping** | 最大出力/プロンプト制限、オーバーフロー時の動作、リトライポリシー |
| **F) Rate limiting** | リクエスト数/分、トークン数/分、バースト許容量 |
| **G) Presets** | 静音（ノートパソコン）、バランス、パフォーマンス（デスクトップ） |
| **H) Diagnostics** | 診断情報のエクスポート、イベントログの表示、ステータスサマリーのコピー |

## 備考

- Lite版では、承認、署名、レシートなど、高度な管理機能は意図的に省略されています。
- デーモンは、実際のWindowsシステムメトリクス（PerformanceCounterによるCPU使用率、GlobalMemoryStatusExによるメモリ使用率）を読み込み、キューの負荷状況をシミュレーションします。
- テストでは、依存性注入によって`FakeSystemMetrics`プロバイダーを使用し、ハードウェアに依存しない、決定的な検証を行っています。

---

<p align="center">
  Built by <a href="https://mcp-tool-shop.github.io/">MCP Tool Shop</a>
</p>
