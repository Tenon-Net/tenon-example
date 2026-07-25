using TenonAdmin.Core;
using TenonAdmin.Services;
using TenonAdmin.SqlSugar;

namespace tenon_example.Modules.Crm.Seeds;

/// <summary>
/// CRM 演示角色的数据范围配置。<c>Org</c>/<c>OrgAndChildren</c> 按用户自己的主属机构解析
/// (见内核 <c>DataScopeProvider</c>),故这里不填机构——机构定在 <see cref="CrmUserSeed"/> 的 <c>OrgId</c> 上。
/// </summary>
public sealed class CrmRoleDataScopeSeed : ISeedData<SysRoleDataScope>
{
    /// <summary>连接表:唯一索引在 RoleId,按 RoleId 判存(同内核 DefaultDataScopeSeed)。</summary>
    public string[] DedupColumns => [nameof(SysRoleDataScope.RoleId)];

    public IEnumerable<SysRoleDataScope> HasData() =>
    [
        new SysRoleDataScope { Id = 1000, RoleId = CrmRoleSeed.HqAdminRoleId, ScopeType = DataScopeType.All, CustomOrgIds = "" },
        new SysRoleDataScope { Id = 1001, RoleId = CrmRoleSeed.SouthManagerRoleId, ScopeType = DataScopeType.OrgAndChildren, CustomOrgIds = "" },
        new SysRoleDataScope { Id = 1002, RoleId = CrmRoleSeed.ShenzhenSpecialistRoleId, ScopeType = DataScopeType.Org, CustomOrgIds = "" },
    ];
}
