<!-- README.zh-CN.md (canonical) と同期を保つこと -->

[English](README.md) | [简体中文](README.zh-CN.md) | 日本語

# Tenon Example

`tenon-example` は TenonAdmin の公開リファレンス消費者アプリケーションです。単一リポジトリで構成される、成長し続けるマルチモジュール業務システムであり、CRM がその最初のフラッグシップモジュールです。本リポジトリ自体は再利用可能なカーネルやサテライトパッケージの機能を開発しません。

## リリースの来歴

本リポジトリは現在、安定版 `0.3.3` に固定されています:

- NuGet: `TenonAdmin` `0.3.3` および `TenonAdmin.Templates` `0.3.3`。
- ソース: TenonAdmin タグ `v0.3.3`。
- バックエンド: `dotnet new tenon-app` で生成。`Dockerfile` は `TenonAdmin.Templates@0.3.3` が同梱する修正を、テンプレートのリリース自体より先に直接取り込み済み([v0.3.3 アップグレード記録](docs/v0.3.3-upgrade.md)参照)。
- フロントエンド: `Tenon-Net/TenonAdmin/web#v0.3.2` から抽出し、以降変更なし(`v0.3.3` は `web/` 配下に上流の変更なし)。

`tenon-example.csproj` の `TenonAdmin` PackageReference は、上記のリリース来歴と厳密に一致していなければなりません。段階的な作業内容と検証エビデンスは[アプリ台帳](docs/app-ledger.md)を参照してください。

## 前提条件

- .NET SDK 10
- Node.js 22 および npm

## バックエンド

```powershell
dotnet restore
dotnet build -c Release
dotnet run
```

`Properties/launchSettings.json` は `ASPNETCORE_ENVIRONMENT=Development` を固定しています。これがないとホストは `Production` として解決され、設計上 CodeFirst による自動スキーマ作成が無効化され、シードテーブル不足で起動に失敗します。このファイルは削除しないでください。デフォルト設定は SQLite を使用します。初回起動時にスキーマを作成し、ランダムなスーパー管理者パスワードをコンソールに出力します(詳細は[v0.3.2 アップグレード記録](docs/v0.3.2-upgrade.md))。バックエンド起動後、ライブネス・レディネス・開発用 OpenAPI コントラクトはそれぞれ `/health`、`/health/ready`、`/openapi/v1.json` で利用できます。

## フロントエンド

先にバックエンドを起動してください。このマシン上でバックエンドとフロントエンドの検証プロセスを同時に実行しないでください。

```powershell
Set-Location web
npm install
npm run gen:api
npm run typecheck
npm run lint
npm run dev
```

フロントエンドの開発サーバーは API と OpenAPI コントラクトを `http://localhost:5100` にプロキシします。

## Docker

```bash
docker compose up -d --build
```

MySQL、Redis、本バックエンド、そして Caddy でホストされた `web/` の本番ビルドという、フルスタックをビルド・実行します。シークレット(`TENON_DB_PASSWORD`、`TENON_JWT_SECRET`、`TENON_ADMIN_PASSWORD`)とポート(`TENON_API_PORT`、`TENON_WEB_PORT`)は `docker-compose.yml` と同じ場所に置いた `.env` ファイルで上書きしてください——実際の値はコミットしないこと。フロントエンドコンテナは `TENON_WEB_PORT`(デフォルト `8090`)で待ち受け、`/api` と `/health*` をバックエンド自身にリバースプロキシします。

## CRM モジュール:マルチ組織データスコープの実演

同じ `GET /api/v1/biz/customer/page` リクエストでも、ログインしたユーザーによって返される行数が変わります。しかも `CustomerService` には組織フィルタリングのコードは一切書かれていません——すべてカーネルのグローバルクエリフィルターが行っています。**[tenonadmin.52moyu.net](https://tenonadmin.52moyu.net/login)** で実際に試すか、自分で実行して以下のいずれかの体験用アカウントでログインし、**客户管理 / Customers** を開いてください:

| アカウント | パスワード | データスコープ | 表示行数 | その他 |
| --- | --- | --- | --- | --- |
| `总部管理员`(本社管理者) | `Trial@123456` | 全組織 | 214 | 完全な**系统(system)**管理コンソール——組織/ユーザー/ロール/メニュー/辞書/設定/ログ/ファイル管理。(`superAdmin` のバイパスではなく)実際に権限付与された本物のロール |
| `华南区域经理`(華南地区マネージャー) | `Trial@123456` | 華南地区とその配下 | 128 | CRM のみ |
| `深圳专员`(深圳担当) | `Trial@123456` | 深圳支店のみ | 42 | CRM のみ |
| `superAdmin` | `TenonExample@675b52d8` | 無制限(スコープをバイパス) | 214 | 全モジュール(system + crm + カーネル同梱のサンプル業務モジュール)、あらゆる画面でフル CRUD |

![本社管理者は全214行を閲覧できる](docs/assets/hq-admin-214.png)
![華南地区マネージャーは128行、地区とその配下にスコープされている](docs/assets/south-manager-128.png)
![深圳担当は自分の42行のみ閲覧できる](docs/assets/shenzhen-specialist-42.png)

3つの業務ロールアカウントは顧客関連エンドポイントに対して読み取り権限のみを付与されている(シードデータは [P2](docs/app-ledger.md) 参照)ため、追加/編集/削除ボタンはそもそも存在しません——これはクライアント側の見せかけではなく、実際の権限差です。本社管理者が **系统** モジュールへアクセスできるのは意図的にその逆のケースで、実際の消費者が管理者向けに設定するのと同様に、メニュー駆動の `SysRoleMenu` 行によって完全に権限付与された本物のロールであり、すべてのボタンが表示されます。これは、本リファレンスアプリがカーネルの完全な標準管理システム機能に CRM を上乗せしたものであり、CRM 専用ツールではないことを示すためのものです。ログインページのワンクリックアカウントボタンは、この4つすべてをカバーしています。

### デモモード(共有・公開デプロイ向けの読み取り専用モード)

`TenonAdmin:DemoMode=true`(環境変数 `TenonAdmin__DemoMode=true`、または `appsettings.json` に設定)を有効にすると、`superAdmin` を含むすべてのアカウントによる `GET` 以外のリクエストが、エラーコード `41002` とともに `403` を返すようになります。これはサーバー側のグローバルフィルターであり、UI 上の取り決めではありません。ローカル開発や評価時は未設定(デフォルト)のままで構いません——体験用アカウント自体の読み取り専用権限だけで、共有デモとしての一貫性は十分に保たれます。

[tenonadmin.52moyu.net](https://tenonadmin.52moyu.net/login) は実際にこの構成で稼働しています:本リポジトリの `docker-compose.yml` がフルスタック(MySQL + Redis + バックエンド + Caddy ホストのフロントエンド)をビルドし、本番デプロイではさらにサーバーローカルの `docker-compose.override.yml` を重ねて `DemoMode` を有効化しています——デプロイ記録、バックアップ、ロールバック手順は[アプリ台帳](docs/app-ledger.md)の P4 セクションを参照してください。

## 再現可能な作成手順

空の親ディレクトリから、上記に記録された成果物を使って:

```powershell
dotnet new install TenonAdmin.Templates@0.3.3
dotnet new tenon-app --output tenon-example
Set-Location tenon-example
npx degit Tenon-Net/TenonAdmin/web#v0.3.3 web
```

その後、本ファイルのバックエンド・フロントエンドの手順に従ってください。P0 検証記録と消費者としての発見事項は `docs/` 配下で管理されています。
