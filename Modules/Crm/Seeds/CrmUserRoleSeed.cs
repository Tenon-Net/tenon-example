using TenonAdmin.Services;
using TenonAdmin.SqlSugar;

namespace tenon_example.Modules.Crm.Seeds;

/// <summary>CRM 试用账号 ↔ 角色绑定(一人一角色一范围)。</summary>
public sealed class CrmUserRoleSeed : ISeedData<SysUserRole>
{
    /// <summary>连接表:代理主键是运行时雪花号、会漂,按 (UserId, RoleId) 判存(同内核 DefaultUserRoleSeed)。</summary>
    public string[] DedupColumns => [nameof(SysUserRole.UserId), nameof(SysUserRole.RoleId)];

    public IEnumerable<SysUserRole> HasData() =>
    [
        new SysUserRole { Id = 1000, UserId = CrmUserSeed.HqAdminUserId, RoleId = CrmRoleSeed.HqAdminRoleId },
        new SysUserRole { Id = 1001, UserId = CrmUserSeed.SouthManagerUserId, RoleId = CrmRoleSeed.SouthManagerRoleId },
        new SysUserRole { Id = 1002, UserId = CrmUserSeed.ShenzhenSpecialistUserId, RoleId = CrmRoleSeed.ShenzhenSpecialistRoleId },
    ];
}
