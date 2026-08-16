<!-- README.zh-CN.md（中国語が基準版）と同期を保つこと -->

[English](README.md) | [简体中文](README.zh-CN.md) | 日本語

<h1 align="center">Tenon Example</h1>

<p align="center">
  <em>TenonAdmin の公開リファレンスアプリ：パッケージを入れ、本番に載せ、今すぐ触れる本物のバックオフィス。</em>
</p>

<p align="center">
  <a href="https://tenonadmin.52moyu.net/login"><strong>🔗 オンラインデモ</strong></a>&nbsp;&nbsp;·&nbsp;&nbsp;<a href="https://github.com/Tenon-Net/TenonAdmin"><strong>📦 TenonAdmin 本体</strong></a>&nbsp;&nbsp;·&nbsp;&nbsp;<a href="docs/app-ledger.md"><strong>📋 実行台帳</strong></a>
</p>

---

## 🎨 これは何？

TenonAdmin を入れただけの、ごく普通の業務システムです。カーネルの一部ではありませんし、「デモのために」書かれた行は一つもありません——TenonAdmin を採用したあなたのコードも、こう見えるはずです。

存在理由は「試す価値があるか判断するために、まず 3 日ドキュメントを読む」という手順を省くこと。触れるデプロイがあり、クローンして動かせるリポジトリがあります。CRM は最初の業務モジュールで、この先さらに増えていきます。再利用可能なカーネル機能はここでは一切開発しません。それは [TenonAdmin](https://github.com/Tenon-Net/TenonAdmin) リポジトリの担当です。

## 🔍 目玉：同じクエリ、3 つの数字

[オンラインデモ](https://tenonadmin.52moyu.net/login)に以下のいずれかのアカウントでログインし、**客户管理（顧客管理）**を開いてください：

| アカウント | パスワード | データスコープ | 件数 | ほかに見えるもの |
| --- | --- | --- | --- | --- |
| `总部管理员`（本社管理者） | `Trial@123456` | 全組織 | 214 | CRM ＋ システム管理一式 |
| `华南区域经理`（華南地区マネージャー） | `Trial@123456` | 華南地区とその配下 | 128 | CRM のみ |
| `深圳专员`（深圳担当） | `Trial@123456` | 深圳支店のみ | 42 | CRM のみ |
| `superAdmin` | `TenonExample@675b52d8` | 無制限 | 214 | 全モジュール、全ボタン |

![本社管理者には 214 件すべてが見える](docs/assets/hq-admin-214.png)
![華南地区マネージャーには 128 件](docs/assets/south-manager-128.png)
![深圳担当には 42 件](docs/assets/shenzhen-specialist-42.png)

3 つの数字は同じエンドポイント、同じフロントエンドコードから出ています。しかも、それを叩く `CustomerService` には組織フィルターが 1 行もありません。カーネルがビジネスコードの外側でフィルターを掛けているからです。どこで掛かっているのか、そしてなぜそれが「数行短く書ける」よりはるかに価値があるのかは、[同じクエリ、3 つの数字](docs/showcase-multi-org-data-scope.md)（中国語）にまとめてあります。

上 3 つのアカウントは顧客エンドポイントに読み取り権限しか持たないため、追加・編集・削除ボタンはそもそもレンダリングされません。本社管理者はさらにカーネル標準のシステム管理メニュー一式を持っていますが、これはスーパー管理者のバイパスではなく通常のロール付与によるものです。ログインページにはこの 4 つのワンクリックボタンがあるので、パスワードを打つ必要はありません。

## 🚀 動かす

.NET 10 SDK が必要です。フロントエンドも動かすなら Node.js 22 も。

### Docker

```bash
docker compose up -d --build
```

MySQL、Redis、バックエンド、Caddy 配信のフロントエンドが一度に立ち上がります。フロントエンドは `TENON_WEB_PORT`（デフォルト `8090`）で待ち受け、`/api` と `/health*` をバックエンドへリバースプロキシします。シークレットとポートは `docker-compose.yml` の隣に置く `.env` で上書きしてください（`TENON_DB_PASSWORD`、`TENON_JWT_SECRET`、`TENON_ADMIN_PASSWORD`、`TENON_API_PORT`、`TENON_WEB_PORT`）。実際の値は絶対にコミットしないこと。

### ローカル開発

```powershell
dotnet restore
dotnet build -c Release
dotnet run
```

デフォルトは SQLite なので、先に DB を用意する必要はありません。初回起動でスキーマを作成し、シードデータを投入し、ランダムな管理者パスワードをコンソールに出力します。起動後は `/health`、`/health/ready`、`/openapi/v1.json` にそのままアクセスできます。

`Properties/launchSettings.json` は削除しないでください。これが `ASPNETCORE_ENVIRONMENT=Development` を固定しています。無いとホストは `Production` として解決され、CodeFirst の自動スキーマ作成が設計上オフになり、シードテーブルが無いまま起動に失敗します。経緯は [v0.3.2 アップグレード記録](docs/v0.3.2-upgrade.md)に。

フロントエンドは別のターミナルで。バックエンドの検証プロセスとメモリを取り合わせないように：

```powershell
Set-Location web
npm install
npm run gen:api
npm run dev
```

dev サーバーが API と OpenAPI コントラクトを `http://localhost:5100` へプロキシします。コミット前に走らせるのは `npm run typecheck` と `npm run lint` の 2 本。

## 🔒 デモモードとインポート体験

顧客インポートはデフォルトで **dry-run**（`CrmDemo:ImportDryRun=true`）です。体験アカウントはアップロード / プレビュー / 検証 / 送信まで一通り使えますが、送信結果は「DB に書いていない」と明示されます。エクスポートは一覧と同じクエリで、データ権限も効いたままです。

公開デモではさらに `CrmDemo:ReadOnly`（`DemoReadOnlyFilter`）を有効にしています。ログインとインポート以外の非 GET はすべて `403` / `41002` を返します。メニューもボタンもフォームも今までどおり描画され、押せます。DB に届かないだけです。**上の 4 アカウントのパスワードは公開されているので、公開デモを守っているのはパスワードではなくこのゲートです。** カーネルの `TenonAdmin:DemoMode` で代用しないでください。あちらは許可リストを持たず、インポートの POST まで止めてしまいます。

ローカルでは `ReadOnly` はデフォルト `false` なので、4 アカウントとも実際に追加・編集・削除できます。`ImportDryRun=false` にすればインポートも本当に書き込みます。

## 📌 バージョン整合

現在は安定版 `0.6.0` に固定しています。NuGet の `TenonAdmin` / `TenonAdmin.Excel` / `TenonAdmin.Templates` が `0.6.0`、ソースは tag `v0.6.0`；フロントは `Tenon-Net/TenonAdmin/web#v0.6.0`。詳細は [v0.6.0 アップグレード記録](docs/v0.6.0-upgrade.md)。

`tenon-example.csproj` のバージョンは、この段落と厳密に一致していなければなりません。カーネルがリリースされるたびにここも上げて検証し直します——このリポジトリはカーネルの常設インテグレーションカナリアも兼ねており、バージョンが古いカナリアは籠に入っていないのと同じだからです。

空のディレクトリから同じ成果物を再現する手順：

```powershell
dotnet new install TenonAdmin.Templates@0.6.0
dotnet new tenon-app --output tenon-example
Set-Location tenon-example
npx degit Tenon-Net/TenonAdmin/web#v0.6.0 web
```

あとは上の 2 節に従ってください。段階ごとの実装記録、検証エビデンス、消費者としてハマった点の一覧はすべて `docs/` にあります。
