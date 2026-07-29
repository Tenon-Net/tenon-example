using Microsoft.Extensions.DependencyInjection.Extensions;
using TenonAdmin.AspNetCore;
using TenonAdmin.Excel;
using TenonAdmin.Services;
using TenonAdmin.SqlSugar;
using tenon_example.Modules.Crm;
using tenon_example.Modules.Crm.Seeds;

// TenonAdmin 消费方 host。首次启动控制台会打印随机超管密码,用它登录。
// 加业务模块:仿 Modules/Crm 的结构新建一个模块目录,并在下方追加一行 TryAddScoped。
var builder = WebApplication.CreateBuilder(args);

// CRM 演示开关(导入 dry-run 默认 true,见 CrmDemoOptions)
builder.Services.Configure<CrmDemoOptions>(builder.Configuration.GetSection(CrmDemoOptions.SectionName));

// 可选卫星 codec 必须在 AddTenonAdmin 之前(TryAdd 先到者胜);不装则任意 xlsx 调用 46001。
builder.Services.AddTenonAdminExcel();

// 注册内核,并把本程序集登记为业务程序集:其中的 [SugarTable] 实体自动建表、[ApiController] 控制器自动挂路由。
builder.Services.AddTenonAdmin(builder.Configuration,
    o => o.ApplicationAssemblies.Add(typeof(Program).Assembly));

// 业务服务:内核内置服务用 TryAdd 可被覆盖;你自己的服务在此显式登记(每个模块一行)。
// CRM 模块(§2 头条:多机构数据范围 + 导入导出体验)。
builder.Services.TryAddScoped<ICustomerService, CustomerService>();
builder.Services.TryAddScoped<CustomerImportProfile>();
builder.Services.TryAddScoped<CustomerExportProfile>();
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

var app = builder.Build();
app.MapTenonAdmin();
app.Run();
