using TenonAdmin.Services;
using TenonAdmin.SqlSugar;

namespace tenon_example.Modules.Crm.Seeds;

/// <summary>
/// 把客户管理的三个只读权限授予全部三个 CRM 试用角色——三个账号的差异纯粹是数据范围,
/// 不是功能权限(§2 头条:同一菜单、同一按钮,行不同)。
/// </summary>
public sealed class CrmRoleMenuSeed : ISeedData<SysRoleMenu>
{
    /// <summary>连接表:唯一索引在 (RoleId, MenuId),按此判存(同内核角色菜单授权语义)。</summary>
    public string[] DedupColumns => [nameof(SysRoleMenu.RoleId), nameof(SysRoleMenu.MenuId)];

    private static readonly long[] RoleIds =
    [
        CrmRoleSeed.HqAdminRoleId,
        CrmRoleSeed.SouthManagerRoleId,
        CrmRoleSeed.ShenzhenSpecialistRoleId,
    ];

    private static readonly long[] MenuIds =
    [
        CrmMenuSeed.CustomerPageButtonId,
        CrmMenuSeed.CustomerDetailButtonId,
        CrmMenuSeed.CustomerScopeButtonId,
    ];

    public IEnumerable<SysRoleMenu> HasData()
    {
        long id = 1000;
        foreach (var roleId in RoleIds)
            foreach (var menuId in MenuIds)
                yield return new SysRoleMenu { Id = id++, RoleId = roleId, MenuId = menuId };
    }
}
