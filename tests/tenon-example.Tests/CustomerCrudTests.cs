using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using TenonAdmin.Core;
using TenonAdmin.Services;
using TenonAdmin.SqlSugar;
using tenon_example.Modules.Crm;

namespace tenon_example.Tests;

/// <summary>P1 验收——CRM 客户标准 CRUD + 分页,证明 CustomerService 全程不写机构过滤代码即可正常增改删查。</summary>
public class CustomerCrudTests
{
    private static async Task<(ServiceProvider Sp, string DbFile)> BuildAsync()
    {
        var id = $"crm-crud-{Guid.NewGuid():N}";
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
        sp.GetRequiredService<IDataScopeContext>().Current = DataScopeResult.Unrestricted;

        return (sp, dbFile);
    }

    [Fact]
    public async Task Page_add_get_update_delete_round_trip()
    {
        var (sp, dbFile) = await BuildAsync();
        await using var _ = sp;
        using var scope = sp.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ICustomerService>();

        var id = await svc.AddAsync(new CustomerInput { Name = "Acme", Contact = "John", Phone = "123", IntendedAmount = 10_000m, Status = CustomerStatus.New });

        var page = await svc.PageAsync(new CustomerPageInput { Current = 1, Size = 20 });
        Assert.Equal(1, page.Total);
        Assert.Equal("Acme", page.Items[0].Name);

        var byName = await svc.PageAsync(new CustomerPageInput { Current = 1, Size = 20, Name = "cme" });
        Assert.Equal(1, byName.Total);
        var byMiss = await svc.PageAsync(new CustomerPageInput { Current = 1, Size = 20, Name = "nope" });
        Assert.Equal(0, byMiss.Total);

        var loaded = await svc.GetAsync(id);
        Assert.Equal("Acme", loaded.Name);
        Assert.Equal(CustomerStatus.New, loaded.Status);

        await svc.UpdateAsync(id, new CustomerInput { Name = "Acme Renamed", Contact = "John", Phone = "456", IntendedAmount = 20_000m, Status = CustomerStatus.Following });
        var updated = await svc.GetAsync(id);
        Assert.Equal("Acme Renamed", updated.Name);
        Assert.Equal(CustomerStatus.Following, updated.Status);
        Assert.Equal(20_000m, updated.IntendedAmount);

        await svc.DeleteAsync(id);
        var ex = await Assert.ThrowsAsync<AdminException>(() => svc.GetAsync(id));
        Assert.Equal((int)BizErrorCode.CustomerNotFound, (int)ex.Code);

        TestDb.Cleanup(dbFile);
    }

    [Fact]
    public async Task GetAsync_throws_for_nonexistent_id()
    {
        var (sp, dbFile) = await BuildAsync();
        await using var _ = sp;
        using var scope = sp.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ICustomerService>();

        var ex = await Assert.ThrowsAsync<AdminException>(() => svc.GetAsync(999_999_999));
        Assert.Equal((int)BizErrorCode.CustomerNotFound, (int)ex.Code);

        TestDb.Cleanup(dbFile);
    }
}
