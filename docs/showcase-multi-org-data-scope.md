# 同一个查询，三个数字

```csharp
public virtual async Task<PagedList<Customer>> PageAsync(CustomerPageInput input) =>
    await customers.AsQueryable()
        .WhereIF(!string.IsNullOrEmpty(input.Name), c => c.Name.Contains(input.Name!))
        .ToPagedListAsync(input, q => q.OrderByDescending(c => c.Id));
```

这是客户列表的全部查询代码（`Modules/Crm/CustomerService.cs`）。除了一个按名称模糊搜索的可选条件，它什么都没做。可总部管理员打开这个页面看到 214 条，华南区域经理 128 条，深圳专员 42 条。

| 账号 | 密码 | 数据范围 | 看到几条 |
| --- | --- | --- | --- |
| `总部管理员` | `Trial@123456` | 全部机构 | 214 |
| `华南区域经理` | `Trial@123456` | 华南大区及其下属分部 | 128 |
| `深圳专员` | `Trial@123456` | 仅深圳分部 | 42 |
| `superAdmin` | `TenonExample@675b52d8` | 不受限 | 214 |

![总部管理员看到全部 214 行](assets/hq-admin-214.png)
![华南区域经理看到 128 行](assets/south-manager-128.png)
![深圳专员看到 42 行](assets/shenzhen-specialist-42.png)

## 那个 WHERE 在哪

在 `CustomerService.cs` 里搜 `CreateOrgId`、搜 `org`，一处都搜不到，唯一命中的是类头上那行注释，写着这里不做机构过滤。

过滤挂在 `Customer` 的基类上。它继承 `DataEntity`，于是带上了 `CreateOrgId` 这个锚点字段，也就落进了内核给所有 `IOrgScoped` 实体注册的全局查询过滤器。剩下三步全发生在业务代码之外：

1. 鉴权阶段，内核把当前登录人的角色数据范围（全部 / 本级及下属 / 本级 / 仅本人 / 自定义）解析成一组可见机构 ID，放进 `IDataScopeContext`。
2. 这个请求触发的任何一次 SqlSugar 查询，只要实体是 `IOrgScoped`，生成 SQL 时自动追加 `CreateOrgId IN (…)`。
3. 写入时 AOP 把 `CreateOrgId` 填成当前用户的机构，新数据自动落在正确的范围里。

`PageAsync` 全程不知道有这回事。三个账号看到不同的行数，不是因为它判断了谁在看，是因为它根本没有机会判断。

值钱的地方也在这里。手写一次机构过滤不难，难的是三十个 service、两百个查询里没有一处漏掉。漏一处就是一个越权，而越权不会在测试环境自己报警，它只是安静地多返回几行数据，等着上线之后被人发现。业务代码没有过滤可写，也就没有地方可漏。

## 按钮的差别也是真的

前三个账号在客户接口上只有读权限，列表页上没有新增、编辑、删除按钮。不是禁用变灰，是根本没渲染：`[RolePermission]` 按路由授权，前端拿到的菜单树里就不含那三个按钮节点。

总部管理员反过来，除了 CRM 还被授予了内核自带的整套系统管理菜单：组织、用户、角色、菜单、字典、配置、日志、文件，所有按钮都在。它走的是和真实项目一样的角色授权，`superAdmin` 那条绕过路径没参与。这也是这个仓库想说明的第二件事：它演示的是内核完整的后台能力，CRM 只是长在上面的第一个业务模块。

上面说的都是真授权，总部管理员那套系统管理按钮点下去是真会执行的。所以线上另外挂了一道闸门（`CrmDemo:ReadOnly`）：除登录和导入外，一切非 GET 返回 `403` / `41002`。按钮照常渲染、照常点，只是够不着数据库。四个账号的密码就写在 README 里，公开演示靠的是这道闸门，不是密码。唯一放行的写路径是导入向导，它跑 dry-run（`CrmDemo:ImportDryRun=true`），上传、预览、校验都是真的，最后一步不落库。

## 自己跑一遍

```bash
git clone https://github.com/Tenon-Net/tenon-example.git
cd tenon-example
docker compose up -d --build
```

不想装 Docker 就按 [README](../README.zh-CN.md) 的后端和前端两节走，`dotnet run` 加 `npm run dev`，默认 SQLite，不用先准备数据库。本地用超管登录，增删改查都真的能点、真的落库，可以自己改一条客户数据再换个账号看它消不消失；把 `CrmDemo:ImportDryRun` 设成 `false`，导入也会真的写进去。

线上那份和你克隆下来的是同一份代码，区别只在服务器本地那两个不进 git 的文件：`.env` 放密钥和端口，`docker-compose.override.yml` 把 `CrmDemo__ReadOnly` 打开。部署、备份和回滚记录在[执行台账](app-ledger.md)的 P4。
