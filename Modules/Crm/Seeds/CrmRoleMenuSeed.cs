using TenonAdmin.Services;
using TenonAdmin.SqlSugar;

namespace tenon_example.Modules.Crm.Seeds;

/// <summary>
/// 把客户管理的只读 + 导入导出权限授予全部三个 CRM 试用角色——三个账号的差异纯粹是数据范围,
/// 不是功能权限(§2 头条:同一菜单、同一按钮,行不同)。
/// <para>Id 用固定公式 <c>2000 + roleIndex * 20 + menuIndex</c>,避免升级时与旧 1000–1008 主键撞车
/// (连接表按 RoleId+MenuId 判存,但 INSERT 仍要带唯一主键)。</para>
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
        CrmMenuSeed.CustomerImportPreviewButtonId,
        CrmMenuSeed.CustomerImportValidateButtonId,
        CrmMenuSeed.CustomerImportErrorReportButtonId,
        CrmMenuSeed.CustomerImportCommitButtonId,
        CrmMenuSeed.CustomerExportButtonId,
    ];

    public IEnumerable<SysRoleMenu> HasData()
    {
        for (var ri = 0; ri < RoleIds.Length; ri++)
        {
            for (var mi = 0; mi < MenuIds.Length; mi++)
            {
                yield return new SysRoleMenu
                {
                    Id = 2000 + ri * 20 + mi,
                    RoleId = RoleIds[ri],
                    MenuId = MenuIds[mi],
                };
            }
        }
    }
}
