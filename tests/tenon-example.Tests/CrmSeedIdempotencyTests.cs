using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SqlSugar;
using TenonAdmin.AspNetCore;
using TenonAdmin.Core;
using TenonAdmin.Services;
using TenonAdmin.SqlSugar;
using tenon_example.Modules.Crm;
using tenon_example.Modules.Crm.Seeds;

namespace tenon_example.Tests;

/// <summary>
/// P2 验收——CRM 种子跑两遍(模拟服务重启)不漂移:行数、固定 Id、角色授权与演示密码保持不变,
/// 三个账号仍然分别看到 214/128/42。用真实 <see cref="IHostedService"/> 管道(而非手工 <c>CodeFirst.InitTables</c>),
/// 因为这正是生产重启时实际跑的那条路径。
/// </summary>
public class CrmSeedIdempotencyTests
{
    private static async Task<IHost> BuildAndStartAsync(string connectionString)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Environment.EnvironmentName = Environments.Development; // CodeFirst 生产建表安全闸门(§12)只放行非生产环境

        // 走真实的 AddTenonAdmin 组合根(与 Program.cs 一致),而非手工拼 AddTenonAdminSqlSugar +
        // AddTenonAdminServices——那样会漏绑一串 Options(Upload/Jwt/Cors/...),越补越漏。
        builder.Configuration.AddInMemoryCollection(
        [
            new("TenonAdmin:Database:DbType", "Sqlite"),
            new("TenonAdmin:Database:ConnectionString", connectionString),
        ]);
        builder.Services.AddTenonAdmin(builder.Configuration, o => o.ApplicationAssemblies.Add(typeof(Customer).Assembly));
        builder.Services.AddScoped<ICustomerService, CustomerService>();

        // AddTenonAdmin 把 IDataScopeContext 换成 HttpContextDataScopeContext(挂 HttpContext.Items)——
        // 没有真实请求时它的 setter 静默空操作、getter 恒返回 Unrestricted。测试要手动切换范围,
        // 换回 SqlSugar 层的纯内存实现(文档里说的"非 HTTP 场景回退"),放在 AddTenonAdmin 之后覆盖生效。
        builder.Services.AddSingleton<IDataScopeContext, DataScopeContext>();

        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<ISeedData, CrmModuleSeed>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<ISeedData, CrmOrgSeed>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<ISeedData, CrmRoleSeed>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<ISeedData, CrmRoleDataScopeSeed>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<ISeedData, CrmUserSeed>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<ISeedData, CrmUserRoleSeed>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<ISeedData, CrmMenuSeed>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<ISeedData, CrmRoleMenuSeed>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<ISeedData, CrmHqAdminSystemMenuSeed>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<ISeedData, CrmCustomerSeed>());

        var host = builder.Build();
        await host.StartAsync(); // 跑 DatabaseInitializer(真实 CodeFirst + 全部种子,含内核自带种子)
        return host;
    }

    private static async Task<(int OrgCount, int RoleCount, int UserCount, int RoleMenuCount, int CustomerCount, int Hq, int South, int Sz, string HqPasswordHash)>
        SnapshotAsync(IHost host)
    {
        using var scope = host.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var ctx = sp.GetRequiredService<IDataScopeContext>();
        ctx.Current = DataScopeResult.Unrestricted;

        var orgCount = await sp.GetRequiredService<IRepository<SysOrg>>().AsQueryable().CountAsync();
        var roleCount = await sp.GetRequiredService<IRepository<SysRole>>().AsQueryable().CountAsync();
        var userCount = await sp.GetRequiredService<IRepository<SysUser>>().AsQueryable().CountAsync();
        var roleMenuCount = await sp.GetRequiredService<IRepository<SysRoleMenu>>().AsQueryable().CountAsync();
        var customerCount = await sp.GetRequiredService<IRepository<Customer>>().AsQueryable().CountAsync();
        var hqUser = await sp.GetRequiredService<IRepository<SysUser>>().GetByIdAsync(CrmUserSeed.HqAdminUserId);

        var dataScopeProvider = sp.GetRequiredService<IDataScopeProvider>();
        var customers = sp.GetRequiredService<ICustomerService>();

        async Task<int> TotalForAsync(long userId)
        {
            ctx.Current = await dataScopeProvider.ResolveAsync(userId);
            var page = await customers.PageAsync(new CustomerPageInput { Current = 1, Size = 1 });
            return page.Total;
        }

        var hqTotal = await TotalForAsync(CrmUserSeed.HqAdminUserId);
        var southTotal = await TotalForAsync(CrmUserSeed.SouthManagerUserId);
        var szTotal = await TotalForAsync(CrmUserSeed.ShenzhenSpecialistUserId);

        return (orgCount, roleCount, userCount, roleMenuCount, customerCount, hqTotal, southTotal, szTotal, hqUser!.Password);
    }

    [Fact]
    public async Task Two_startups_against_the_same_database_do_not_drift()
    {
        var id = $"crm-p2-{Guid.NewGuid():N}";
        var dbFile = Path.Combine(Path.GetTempPath(), $"tenon-example-{id}.db");
        var connectionString = $"Data Source={dbFile}";

        var host1 = await BuildAndStartAsync(connectionString);
        var first = await SnapshotAsync(host1);
        await host1.StopAsync();
        host1.Dispose();

        var host2 = await BuildAndStartAsync(connectionString);
        var second = await SnapshotAsync(host2);
        await host2.StopAsync();
        host2.Dispose();

        Assert.Equal(first.OrgCount, second.OrgCount);
        Assert.Equal(first.RoleCount, second.RoleCount);
        Assert.Equal(first.UserCount, second.UserCount);
        Assert.Equal(first.RoleMenuCount, second.RoleMenuCount);
        Assert.Equal(first.CustomerCount, second.CustomerCount);
        Assert.Equal(first.HqPasswordHash, second.HqPasswordHash); // 密码哈希未被第二次启动重新生成/覆盖

        Assert.Equal(214, first.Hq);
        Assert.Equal(128, first.South);
        Assert.Equal(42, first.Sz);
        Assert.Equal(214, second.Hq);
        Assert.Equal(128, second.South);
        Assert.Equal(42, second.Sz);

        TestDb.Cleanup(dbFile);
    }
}
