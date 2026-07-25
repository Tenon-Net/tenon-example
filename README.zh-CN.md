<!-- 本文件为准确来源(canonical);请与 README.md / README.ja.md 保持同步 -->

[English](README.md) | 简体中文 | [日本語](README.ja.md)

# Tenon Example

`tenon-example` 是 TenonAdmin 的公开参考消费者应用。它是一个单仓库、持续成长的多模块业务系统;CRM 是它的第一个旗舰模块,本仓库本身不开发可复用的内核或卫星包能力。

## 发布溯源

本仓库当前锁定在稳定版 `0.3.3`:

- NuGet:`TenonAdmin` `0.3.3` 与 `TenonAdmin.Templates` `0.3.3`。
- 源码:TenonAdmin 标签 `v0.3.3`。
- 后端:由 `dotnet new tenon-app` 生成;其 `Dockerfile` 已直接采用 `TenonAdmin.Templates@0.3.3` 携带的修复(先于模板发布本身落地——见[v0.3.3 升级记录](docs/v0.3.3-upgrade.md))。
- 前端:提取自 `Tenon-Net/TenonAdmin/web#v0.3.2`,此后未变(`v0.3.3` 在 `web/` 目录下没有任何上游改动)。

`tenon-example.csproj` 中的 `TenonAdmin` PackageReference 必须与上述发布溯源严格保持一致。分阶段的工作与验证证据见[执行台账](docs/app-ledger.md)。

## 前置条件

- .NET SDK 10
- Node.js 22 与 npm

## 后端

```powershell
dotnet restore
dotnet build -c Release
dotnet run
```

`Properties/launchSettings.json` 固定了 `ASPNETCORE_ENVIRONMENT=Development`;缺少它宿主会解析为 `Production`,此时按设计会禁用自动 CodeFirst 建表,启动将因缺少种子表而失败。请不要删除此文件。默认配置使用 SQLite。首次启动会创建数据库结构并在控制台打印一个随机的超级管理员密码,详见[v0.3.2 升级记录](docs/v0.3.2-upgrade.md)。后端运行后,存活探针、就绪探针与开发环境 OpenAPI 契约分别位于 `/health`、`/health/ready`、`/openapi/v1.json`。

## 前端

请先启动后端。不要在同一台机器上同时运行后端和前端的校验进程。

```powershell
Set-Location web
npm install
npm run gen:api
npm run typecheck
npm run lint
npm run dev
```

前端开发服务器会将 API 与 OpenAPI 契约代理到 `http://localhost:5100`。

## Docker

```bash
docker compose up -d --build
```

会构建并运行完整技术栈:MySQL、Redis、本后端,以及由 Caddy 托管的 `web/` 生产构建产物。通过与 `docker-compose.yml` 同目录的 `.env` 文件覆盖密钥(`TENON_DB_PASSWORD`、`TENON_JWT_SECRET`、`TENON_ADMIN_PASSWORD`)与端口(`TENON_API_PORT`、`TENON_WEB_PORT`)——切勿提交真实值。前端容器监听 `TENON_WEB_PORT`(默认 `8090`),并将 `/api` 与 `/health*` 反向代理到后端本身。

## CRM 模块:多组织数据权限实战

同一个 `GET /api/v1/biz/customer/page` 请求,根据登录者不同会返回不同的行数,而 `CustomerService` 里没有任何手写的组织过滤逻辑——都是内核的全局查询过滤器在起作用。可以直接在 **[tenonadmin.52moyu.net](https://tenonadmin.52moyu.net/login)** 上体验,也可以自己跑起来:用下面任意一个体验账号登录,打开 **客户管理 / Customers**。这套机制的完整拆解见[《同一个页面,不同的行数》](docs/showcase-multi-org-data-scope.md)。

| 账号 | 密码 | 数据范围 | 可见行数 | 还能看到 |
| --- | --- | --- | --- | --- |
| `总部管理员`(HQ admin) | `Trial@123456` | 全部组织 | 214 | 完整的**系统 (system)** 管理控制台——组织/用户/角色/菜单/字典/配置/日志/文件管理,一个真正被授权的角色(不是靠 `superAdmin` 绕过) |
| `华南区域经理`(South China regional manager) | `Trial@123456` | 华南大区及其下属机构 | 128 | 仅 CRM |
| `深圳专员`(Shenzhen specialist) | `Trial@123456` | 仅深圳分部 | 42 | 仅 CRM |
| `superAdmin` | `TenonExample@675b52d8` | 不受限(绕过数据范围) | 214 | 全部模块(系统 + crm + 内核自带的示例业务模块),所有页面完整增删改查 |

![总部管理员可见全部 214 行](docs/assets/hq-admin-214.png)
![华南区域经理可见 128 行,范围限定在本大区及其下属机构](docs/assets/south-manager-128.png)
![深圳专员只能看到自己的 42 行](docs/assets/shenzhen-specialist-42.png)

三个业务角色账号在客户相关接口上只被授予了只读权限(种子数据见 [P2](docs/app-ledger.md)),所以新增/编辑/删除按钮对它们而言根本不存在——这是真实的权限差异,不是前端做的样子。总部管理员进入 **系统** 模块则刻意相反:一个真正被授权的完整角色(和真实消费者给管理员配置的一样,基于菜单驱动的 `SysRoleMenu` 记录),所有按钮都可见。这是为了说明这个参考应用展示的是内核完整的开箱即用后台能力**加上** CRM,而不是一个只有 CRM 的工具。登录页的一键登录按钮覆盖全部四个账号。

### 演示模式(只读,适用于共享/公开部署)

设置 `TenonAdmin:DemoMode=true`(例如环境变量 `TenonAdmin__DemoMode=true`,或写在 `appsettings.json` 里)后,所有账号(包括 `superAdmin`)发起的任何非 `GET` 请求都会返回 `403`,错误码 `41002`。这是服务端的全局过滤器,不是前端约定。本地开发与评估时留空(默认值)即可,体验账号自身的只读权限已经足够维持共享演示的叙事完整性。

[tenonadmin.52moyu.net](https://tenonadmin.52moyu.net/login) 就是这样运行的:本仓库的 `docker-compose.yml` 构建完整技术栈(MySQL + Redis + 后端 + Caddy 托管前端),线上部署再叠加一份服务器本地的 `docker-compose.override.yml` 打开 `DemoMode`——部署记录、备份与回滚步骤见[执行台账](docs/app-ledger.md)的 P4 部分。

## 可复现的创建过程

从一个空的父目录开始,使用与上文记录一致的产物:

```powershell
dotnet new install TenonAdmin.Templates@0.3.3
dotnet new tenon-app --output tenon-example
Set-Location tenon-example
npx degit Tenon-Net/TenonAdmin/web#v0.3.3 web
```

然后按本文件的后端与前端命令继续。P0 验证记录与消费者发现维护在 `docs/` 目录下。
