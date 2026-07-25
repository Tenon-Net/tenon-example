using TenonAdmin.Core;
using TenonAdmin.Services;
using TenonAdmin.SqlSugar;

namespace tenon_example.Modules.Crm.Seeds;

/// <summary>
/// CRM 三个试用账号——同一个 <c>GET /api/v1/biz/customer/page</c>,总部管理员看 214 条、
/// 华南区域经理看 128 条、深圳专员看 42 条(§2 头条)。均为非超级管理员,密码不强制下次改、
/// 不绑手机号,登录后默认进 CRM 模块。密码固定值仅供本地/评估环境使用,公开部署见 P4(DemoMode)。
/// </summary>
public sealed class CrmUserSeed(IPasswordHasher hasher) : ISeedData<SysUser>
{
    /// <summary>三个试用账号共用的演示密码(明文仅存在于本注释与文档,库里只落哈希)。</summary>
    public const string TrialPassword = "Trial@123456";

    public const long HqAdminUserId = 1000;
    public const long SouthManagerUserId = 1001;
    public const long ShenzhenSpecialistUserId = 1002;

    public IEnumerable<SysUser> HasData()
    {
        var password = hasher.Hash(TrialPassword);
        var now = DateTime.UtcNow;

        return
        [
            new SysUser
            {
                Id = HqAdminUserId,
                Account = "总部管理员",
                Name = "总部管理员",
                Password = password,
                OrgId = CrmOrgSeed.HeadquartersId,
                DefaultModuleId = CrmModuleSeed.ModuleId,
                Enabled = true,
                IsSuperAdmin = false,
                MustChangePassword = false,
                LastPasswordChangeTime = now,
            },
            new SysUser
            {
                Id = SouthManagerUserId,
                Account = "华南区域经理",
                Name = "华南区域经理",
                Password = password,
                OrgId = CrmOrgSeed.SouthRegionId,
                DefaultModuleId = CrmModuleSeed.ModuleId,
                Enabled = true,
                IsSuperAdmin = false,
                MustChangePassword = false,
                LastPasswordChangeTime = now,
            },
            new SysUser
            {
                Id = ShenzhenSpecialistUserId,
                Account = "深圳专员",
                Name = "深圳专员",
                Password = password,
                OrgId = CrmOrgSeed.ShenzhenId,
                DefaultModuleId = CrmModuleSeed.ModuleId,
                Enabled = true,
                IsSuperAdmin = false,
                MustChangePassword = false,
                LastPasswordChangeTime = now,
            },
        ];
    }
}
