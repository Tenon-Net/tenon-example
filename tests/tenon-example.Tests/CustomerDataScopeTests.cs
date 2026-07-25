using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using TenonAdmin.Core;
using TenonAdmin.Services;
using TenonAdmin.SqlSugar;
using tenon_example.Modules.Crm;

namespace tenon_example.Tests;

/// <summary>
/// P1 验收——数据范围写守卫:直接设置受限 <see cref="IDataScopeContext"/>,证明跨范围详情不可见、
/// 更新/删除被拒(经 <see cref="CustomerService"/>,而非绕过服务层直触仓储的 IDOR 探测——那条通用守卫
/// 已由内核 <c>DataScopeTests.Write_path_blocks_cross_org_update_and_delete</c> 覆盖,这里只证明业务服务
/// 正确把"越权/不存在"归一为 <see cref="BizErrorCode.CustomerNotFound"/>,不靠 <c>[RolePermission]</c> 的
/// 403 冒充数据范围验证——本测试完全绕开 HTTP 层,没有权限过滤器介入)。
/// </summary>
public class CustomerDataScopeTests
{
    private static async Task<(ServiceProvider Sp, string DbFile)> BuildAsync()
    {
        var id = $"crm-scope-{Guid.NewGuid():N}";
        var dbFile = Path.Combine(Path.GetTempPath(), $"tenon-example-{id}.db");
        var services = new ServiceCollection();
        services.AddSingleton(new AdminCacheOptions());
        services.AddTenonAdminSqlSugar(
            new AdminDatabaseOptions { DbType = "Sqlite", ConnectionString = $"Data Source={dbFile}" },
            [typeof(ServicesSetup).Assembly]);
        services.AddTenonAdminServices();
        services.AddScoped<ICustomerService, CustomerService>();
        var sp = services.BuildServiceProvider();

        sp.GetRequiredService<ISqlSugarClient>().CodeFirst.InitTables(typeof(Customer), typeof(SysOrg));
        return (sp, dbFile);
    }

    [Fact]
    public async Task Cross_org_detail_update_and_delete_are_blocked()
    {
        var (sp, dbFile) = await BuildAsync();
        await using var _ = sp;
        var ctx = sp.GetRequiredService<IDataScopeContext>();

        // 不受限装两行,分属机构 10 / 20
        ctx.Current = DataScopeResult.Unrestricted;
        long org10Id, org20Id;
        using (var scope = sp.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Customer>>();
            var a = new Customer { Name = "OrgA-Customer", Contact = "A", IntendedAmount = 1, CreateOrgId = 10 };
            var b = new Customer { Name = "OrgB-Customer", Contact = "B", IntendedAmount = 2, CreateOrgId = 20 };
            await repo.InsertAsync(a);
            await repo.InsertAsync(b);
            org10Id = a.Id;
            org20Id = b.Id;
        }

        // 切到"仅机构 10":机构 20 的客户详情不可见、改/删被拒;机构 10 的客户放行
        ctx.Current = DataScopeResult.Restricted([10], includeSelf: false, userId: 0);
        using (var scope = sp.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<ICustomerService>();

            var detailEx = await Assert.ThrowsAsync<AdminException>(() => svc.GetAsync(org20Id));
            Assert.Equal((int)BizErrorCode.CustomerNotFound, (int)detailEx.Code);

            var updateEx = await Assert.ThrowsAsync<AdminException>(() =>
                svc.UpdateAsync(org20Id, new CustomerInput { Name = "hacked", Contact = "B", IntendedAmount = 999 }));
            Assert.Equal((int)BizErrorCode.CustomerNotFound, (int)updateEx.Code);

            var deleteEx = await Assert.ThrowsAsync<AdminException>(() => svc.DeleteAsync(org20Id));
            Assert.Equal((int)BizErrorCode.CustomerNotFound, (int)deleteEx.Code);

            var own = await svc.GetAsync(org10Id);
            Assert.Equal("OrgA-Customer", own.Name);
            await svc.UpdateAsync(org10Id, new CustomerInput { Name = "OrgA-Renamed", Contact = "A", IntendedAmount = 1 });
            await svc.DeleteAsync(org10Id);
        }

        // 不受限复核:机构 20 客户完好未被越权改删;机构 10 客户确被改名 + 软删
        ctx.Current = DataScopeResult.Unrestricted;
        using (var scope = sp.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Customer>>();
            var b = await repo.GetByIdAsync(org20Id);
            Assert.NotNull(b);
            Assert.Equal("OrgB-Customer", b!.Name);

            Assert.Null(await repo.GetByIdAsync(org10Id));
        }

        TestDb.Cleanup(dbFile);
    }

    [Fact]
    public async Task Page_returns_only_in_scope_rows()
    {
        var (sp, dbFile) = await BuildAsync();
        await using var _ = sp;
        var ctx = sp.GetRequiredService<IDataScopeContext>();

        ctx.Current = DataScopeResult.Unrestricted;
        using (var scope = sp.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Customer>>();
            await repo.InsertRangeAsync(
            [
                new() { Name = "A", Contact = "a", IntendedAmount = 1, CreateOrgId = 10 },
                new() { Name = "B", Contact = "b", IntendedAmount = 1, CreateOrgId = 20 },
                new() { Name = "C", Contact = "c", IntendedAmount = 1, CreateOrgId = 30 },
            ]);
        }

        ctx.Current = DataScopeResult.Restricted([10, 30], includeSelf: false, userId: 0);
        using (var scope = sp.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<ICustomerService>();
            var page = await svc.PageAsync(new CustomerPageInput { Current = 1, Size = 20 });
            Assert.Equal(2, page.Total);
            Assert.DoesNotContain(page.Items, c => c.Name == "B");
        }

        TestDb.Cleanup(dbFile);
    }

    [Theory]
    [MemberData(nameof(ScopeCases))]
    public async Task GetScopeAsync_translates_data_scope_correctly(
        DataScopeResult scope, CustomerScopeKind expectedKind, string? expectedOrgName, int expectedCount, bool expectedIncludeSelf)
    {
        var (sp, dbFile) = await BuildAsync();
        await using var _ = sp;

        // 机构树:总部(1) -> 华南(2) -> 深圳(3)/广州(4);总部 -> 华北(5) -> 北京(6)
        using (var scope2 = sp.CreateScope())
        {
            var orgRepo = scope2.ServiceProvider.GetRequiredService<IRepository<SysOrg>>();
            await orgRepo.InsertRangeAsync(
            [
                new() { Id = 1, ParentId = 0, Name = "总部", Code = "hq" },
                new() { Id = 2, ParentId = 1, Name = "华南", Code = "south" },
                new() { Id = 3, ParentId = 2, Name = "深圳", Code = "sz" },
                new() { Id = 4, ParentId = 2, Name = "广州", Code = "gz" },
                new() { Id = 5, ParentId = 1, Name = "华北", Code = "north" },
                new() { Id = 6, ParentId = 5, Name = "北京", Code = "bj" },
            ]);
        }

        var ctx = sp.GetRequiredService<IDataScopeContext>();
        ctx.Current = scope;
        using (var scope3 = sp.CreateScope())
        {
            var svc = scope3.ServiceProvider.GetRequiredService<ICustomerService>();
            var dto = await svc.GetScopeAsync();

            Assert.Equal(expectedKind, dto.Kind);
            Assert.Equal(expectedOrgName, dto.OrgName);
            Assert.Equal(expectedCount, dto.VisibleOrgCount);
            Assert.Equal(expectedIncludeSelf, dto.IncludeSelf);
        }

        TestDb.Cleanup(dbFile);
    }

    public static TheoryData<DataScopeResult, CustomerScopeKind, string?, int, bool> ScopeCases() => new()
    {
        // 不受限 → 全部组织,机构总数 6
        { DataScopeResult.Unrestricted, CustomerScopeKind.All, null, 6, false },
        // 总部及以下(总部+全部 5 个后代)→ 根机构及以下,机构名=总部
        { DataScopeResult.Restricted([1, 2, 3, 4, 5, 6], includeSelf: false, userId: 0), CustomerScopeKind.OrgAndChildren, "总部", 6, false },
        // 华南及以下(华南+深圳+广州)→ 根机构及以下,机构名=华南
        { DataScopeResult.Restricted([2, 3, 4], includeSelf: false, userId: 0), CustomerScopeKind.OrgAndChildren, "华南", 3, false },
        // 单一机构、无下级 → 机构语义
        { DataScopeResult.Restricted([3], includeSelf: false, userId: 0), CustomerScopeKind.Org, "深圳", 1, false },
        // 两个不相干的叶子机构 → 指定若干机构
        { DataScopeResult.Restricted([3, 6], includeSelf: false, userId: 0), CustomerScopeKind.Specified, null, 2, false },
        // 华南的非整棵子树(缺广州)→ 指定若干机构,不得误判为"华南及以下"
        { DataScopeResult.Restricted([2, 3], includeSelf: false, userId: 0), CustomerScopeKind.Specified, null, 2, false },
        // 空机构范围 + 仅本人 → None,IncludeSelf=true
        { DataScopeResult.Restricted([], includeSelf: true, userId: 42), CustomerScopeKind.None, null, 0, true },
        // 华南及以下 + 仅本人(组合语义,IncludeSelf 与 Kind 正交)
        { DataScopeResult.Restricted([2, 3, 4], includeSelf: true, userId: 42), CustomerScopeKind.OrgAndChildren, "华南", 3, true },
    };
}
