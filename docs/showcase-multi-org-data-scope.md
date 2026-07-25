# 同一个页面,不同的行数:用 tenon-example 看 TenonAdmin 的多组织数据范围

## 先看结果

打开 [tenonadmin.52moyu.net](https://tenonadmin.52moyu.net/login),用下面任意一个账号登录,进 **客户管理 / Customers**,同一个页面、同一段前端代码、同一个 `GET /api/v1/biz/customer/page` 接口:

| 账号 | 密码 | 数据范围 | 看到几条 |
| --- | --- | --- | --- |
| `总部管理员` | `Trial@123456` | 全部组织 | 214 |
| `华南区域经理` | `Trial@123456` | 华南大区及其下属机构 | 128 |
| `深圳专员` | `Trial@123456` | 仅深圳分部 | 42 |
| `superAdmin` | `TenonExample@675b52d8` | 不受限 | 214 |

![总部管理员可见全部 214 行](assets/hq-admin-214.png)
![华南区域经理可见 128 行](assets/south-manager-128.png)
![深圳专员只能看到自己的 42 行](assets/shenzhen-specialist-42.png)

行数不一样,是因为登录的人不一样。这本身不新鲜,任何做过多租户/多机构后台的人都写过这种过滤。新鲜的是下一段。

## 再看代码:这段过滤到底写在哪

`tenon-example` 的 `CustomerService`(`Modules/Crm/CustomerService.cs`)是一个非常普通的 CRUD service——分页、查询、增删改。翻遍它,**找不到一行 `WHERE org_id` 或者任何手写的组织过滤**。`grep` 一下就能确认:唯一命中的是类自己头上的一句文档注释,写着"本类不做任何组织过滤",而不是过滤逻辑本身。

过滤发生在 `Customer` 实体继承的 `DataEntity` 基类上。`DataEntity` 实现了 `IOrgScoped`,而 TenonAdmin 内核在 `SqlSugarSetup` 里为所有 `IOrgScoped` 实体注册了一个**全局查询过滤器**:每一次请求进来,鉴权阶段就会把当前登录人的角色数据范围(`SysRoleDataScope`:全部 / 本级及下属 / 本级 / 仅本人 / 自定义)解析成一组可见机构 ID,存进 `IDataScopeContext`;之后这个请求触发的**所有** SqlSugar 查询,只要目标实体是 `IOrgScoped`,都会被自动拼上这组机构 ID 的过滤条件——不需要业务代码知道这件事,更不需要每个 service 自己拼一遍。

所以三个账号看到不同的行数,不是因为 `CustomerService` 里判断了"你是谁、该看多少",而是因为**它压根不知道有这回事**——过滤在它够不到的地方,已经被内核做完了。这也是为什么这一条能作为 TenonAdmin 的头条:它消灭的不是"多写几行代码"的麻烦,而是一整类**越权(IDOR)** bug——业务代码没机会漏写过滤,因为它根本没有过滤可写。

## 权限差异也是真的,不是摆设

三个业务账号(总部管理员/华南区域经理/深圳专员)在客户接口上只被授予了只读权限,所以页面上新增/编辑/删除按钮**对它们而言不存在**,不是禁用变灰,是真的没有——这是 `[RolePermission]` 按路由授权的结果,同一套机制。反过来,总部管理员被额外授予了内核自带的**系统管理**模块的完整权限(组织/用户/角色/菜单/字典/配置/日志/文件),所有按钮都可见——用来说明这个参考应用展示的是内核完整的开箱即用后台能力**加上** CRM,而不是一个只有 CRM 的玩具。

线上部署开了 `TenonAdmin:DemoMode=true`,所以即使按钮都在,真点下去也会被服务端全局拒绝(`403`,错误码 `41002`)——这是唯一"看得到但做不了"的地方,而且是故意的:为了让这个演示可以放心公开给陌生人写。

## 自己跑一遍

不想只看线上部署,本地也能在几分钟内复现同样的效果:

```bash
git clone https://github.com/Tenon-Net/tenon-example.git
cd tenon-example
docker compose up -d --build
```

或者不用 Docker,按仓库 [README](../README.zh-CN.md) 的「后端」「前端」两节,`dotnet run` + `npm run dev` 直接起本地开发环境——本地默认不开 `DemoMode`,四个账号的增删改查都能真的点下去、真的落库。

## 这一段证明了什么

`CustomerService` 是 `tenon-example` 自己写的业务代码,不是内核代码——它证明的不是"内核这个功能做得好",而是**一个普通消费者接入 TenonAdmin 之后,写业务代码时可以完全不用操心组织权限这件事,内核已经把这类错误的可能性从业务层面拿掉了**。这也是为什么台账(`docs/app-ledger.md`)把这一条,而不是"可替换性"或"零配置启动",定成这个参考应用唯一的头条。
