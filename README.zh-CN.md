<!-- 本文件为 README 的中文基准版；README.md、README.ja.md 以本文件为准同步 -->

[English](README.md) | 简体中文 | [日本語](README.ja.md)

<h1 align="center">Tenon Example</h1>

<p align="center">
  <em>TenonAdmin 的公开参考应用：一个装了包、部署上线、能直接点进去的真实后台。</em>
</p>

<p align="center">
  <a href="https://tenonadmin.52moyu.net/login"><strong>🔗 在线演示</strong></a>&nbsp;&nbsp;·&nbsp;&nbsp;<a href="https://github.com/Tenon-Net/TenonAdmin"><strong>📦 TenonAdmin 内核</strong></a>&nbsp;&nbsp;·&nbsp;&nbsp;<a href="docs/app-ledger.md"><strong>📋 执行台账</strong></a>
</p>

---

## 🎨 这是什么？

一个装了 TenonAdmin 包的普通业务系统。它不是内核的一部分，代码里没有一行是「为了演示」才那么写的，你接入 TenonAdmin 之后写出来的东西就长这样。

存在的理由是省掉「先读三天文档才知道值不值得试」这一步：线上有部署可以直接点，仓库可以直接克隆跑起来。CRM 是它的第一个业务模块，后面还会长出别的。这里不开发任何可复用的内核能力，那属于 [TenonAdmin](https://github.com/Tenon-Net/TenonAdmin) 仓库。

## 🔍 头条：同一个查询，三个数字

用下面任意一个账号登录[在线演示](https://tenonadmin.52moyu.net/login)，打开**客户管理**：

| 账号 | 密码 | 数据范围 | 看到几条 | 还能看到 |
| --- | --- | --- | --- | --- |
| `总部管理员` | `Trial@123456` | 全部机构 | 214 | CRM ＋ 整套系统管理菜单 |
| `华南区域经理` | `Trial@123456` | 华南大区及其下属分部 | 128 | 只有 CRM |
| `深圳专员` | `Trial@123456` | 仅深圳分部 | 42 | 只有 CRM |
| `superAdmin` | `TenonExample@675b52d8` | 不受限 | 214 | 全部模块，全部按钮 |

![总部管理员看到全部 214 行](docs/assets/hq-admin-214.png)
![华南区域经理看到 128 行](docs/assets/south-manager-128.png)
![深圳专员看到 42 行](docs/assets/shenzhen-specialist-42.png)

三个数字来自同一个接口、同一段前端代码，而查询它的 `CustomerService` 里翻不出一行机构过滤。那行过滤是内核在业务代码之外挂上去的，[《同一个查询，三个数字》](docs/showcase-multi-org-data-scope.md)讲了它挂在哪、以及为什么这比「少写几行代码」值钱得多。

前三个账号在客户接口上只有读权限，页面上根本不渲染增删改按钮。总部管理员额外拿到了内核自带的整套系统管理菜单，走的是正常角色授权而非超管绕过。登录页有一键登录按钮，四个账号都不用手打密码。

## 🚀 跑起来

需要 .NET 10 SDK；跑前端还需要 Node.js 22。

### Docker

```bash
docker compose up -d --build
```

MySQL、Redis、后端、Caddy 托管的前端一起起来，前端监听 `TENON_WEB_PORT`（默认 `8090`），并把 `/api` 和 `/health*` 反代给后端。密钥和端口放在 `docker-compose.yml` 旁边的 `.env` 里覆盖（`TENON_DB_PASSWORD`、`TENON_JWT_SECRET`、`TENON_ADMIN_PASSWORD`、`TENON_API_PORT`、`TENON_WEB_PORT`），真实值不要提交。

### 本地开发

```powershell
dotnet restore
dotnet build -c Release
dotnet run
```

默认走 SQLite，不用先装数据库。首次启动自动建表、灌种子数据，并在控制台打印一串随机超管密码。起来之后 `/health`、`/health/ready`、`/openapi/v1.json` 都可以直接访问。

`Properties/launchSettings.json` 别删。它固定了 `ASPNETCORE_ENVIRONMENT=Development`；缺了它宿主按 `Production` 解析，CodeFirst 自动建表会按设计关掉，启动时直接因为找不到种子表而失败。这个坑的来龙去脉在 [v0.3.2 升级记录](docs/v0.3.2-upgrade.md)。

前端另开一个终端，别和后端的校验进程抢内存：

```powershell
Set-Location web
npm install
npm run gen:api
npm run dev
```

dev server 会把 API 和 OpenAPI 契约代理到 `http://localhost:5100`。`npm run typecheck` 和 `npm run lint` 是提交前该跑的两条。

## 🔒 演示模式

`TenonAdmin:DemoMode=true`（环境变量写作 `TenonAdmin__DemoMode=true`）一开，所有账号（含 `superAdmin`）的非 `GET` 请求一律返回 `403`，错误码 `41002`。这是服务端的全局过滤器，不是前端藏几个按钮。

[tenonadmin.52moyu.net](https://tenonadmin.52moyu.net/login) 就这么跑的：本仓库的 `docker-compose.yml` 起完整技术栈，服务器上再叠一份 `docker-compose.override.yml` 把开关打开。部署、备份、回滚记录在[执行台账](docs/app-ledger.md)的 P4。本地开发不用管它，默认是关的。

## 📌 版本对齐

当前锁定在稳定版 `0.3.3`：NuGet 上的 `TenonAdmin` 与 `TenonAdmin.Templates` 都是 `0.3.3`，对应源码 tag `v0.3.3`。后端由 `dotnet new tenon-app` 生成，其中 `Dockerfile` 提前采用了 `0.3.3` 才发布的修复（见 [v0.3.3 升级记录](docs/v0.3.3-upgrade.md)）；前端提取自 `Tenon-Net/TenonAdmin/web#v0.3.2`，此后上游 `web/` 没有任何改动。

`tenon-example.csproj` 里的版本号必须和上面这段严格一致。内核每发一个版本，这里跟着 bump 一次并重验一遍——这个仓库同时也是内核的永久集成金丝雀，版本落后就等于金丝雀没在笼子里。

从空目录复现同一套产物：

```powershell
dotnet new install TenonAdmin.Templates@0.3.3
dotnet new tenon-app --output tenon-example
Set-Location tenon-example
npx degit Tenon-Net/TenonAdmin/web#v0.3.3 web
```

然后照上面「跑起来」两节走。分阶段的实现记录、验证证据和踩坑清单都在 `docs/`。
