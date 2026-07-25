using TenonAdmin.Services;
using TenonAdmin.SqlSugar;

namespace tenon_example.Modules.Crm.Seeds;

/// <summary>
/// CRM 客户管理页菜单节点——根级 Menu(可挂 <see cref="CrmModuleSeed.ModuleId"/>)+ 三个只读按钮。
/// 只播 P1 已实现的三个 GET 接口;新增/更新/删除权限码留给真正需要写权限的角色手工授权
/// (公开演示环境本就只读,见 P4 DemoMode)。
/// </summary>
public sealed class CrmMenuSeed : ISeedData<SysMenu>
{
    public const long CustomerPageMenuId = 1000;
    public const long CustomerPageButtonId = 1001;
    public const long CustomerDetailButtonId = 1002;
    public const long CustomerScopeButtonId = 1003;

    public IEnumerable<SysMenu> HasData() =>
    [
        new SysMenu
        {
            Id = CustomerPageMenuId,
            ParentId = 0,
            Type = MenuType.Menu,
            Title = "客户管理",
            Permission = "",
            Path = "/crm/customer",
            Component = "crm/customer/index",
            Icon = "ph:users-duotone",
            Sort = 1,
            Enabled = true,
            Visible = true,
            ModuleId = CrmModuleSeed.ModuleId,
        },
        new SysMenu { Id = CustomerPageButtonId, ParentId = CustomerPageMenuId, Type = MenuType.Button, Title = "客户-分页", Permission = "GET:/api/v1/biz/customer/page", Sort = 1, Enabled = true },
        new SysMenu { Id = CustomerDetailButtonId, ParentId = CustomerPageMenuId, Type = MenuType.Button, Title = "客户-详情", Permission = "GET:/api/v1/biz/customer/{id}", Sort = 2, Enabled = true },
        new SysMenu { Id = CustomerScopeButtonId, ParentId = CustomerPageMenuId, Type = MenuType.Button, Title = "客户-数据范围", Permission = "GET:/api/v1/biz/customer/scope", Sort = 3, Enabled = true },
    ];
}
