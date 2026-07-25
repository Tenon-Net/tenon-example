using TenonAdmin.Services;
using TenonAdmin.SqlSugar;

namespace tenon_example.Modules.Crm.Seeds;

/// <summary>CRM 演示角色——三个数据范围各一个,绑给同名试用账号(§2 头条:同一菜单,不同人看不同行)。</summary>
public sealed class CrmRoleSeed : ISeedData<SysRole>
{
    public const long HqAdminRoleId = 1000;
    public const long SouthManagerRoleId = 1001;
    public const long ShenzhenSpecialistRoleId = 1002;

    public IEnumerable<SysRole> HasData() =>
    [
        new SysRole { Id = HqAdminRoleId, Name = "总部管理员", Code = "crm_hq_admin", Sort = 1, Enabled = true, Remark = "CRM 演示角色:数据范围=全部" },
        new SysRole { Id = SouthManagerRoleId, Name = "华南区域经理", Code = "crm_south_manager", Sort = 2, Enabled = true, Remark = "CRM 演示角色:数据范围=本机构及以下(华南大区)" },
        new SysRole { Id = ShenzhenSpecialistRoleId, Name = "深圳专员", Code = "crm_sz_specialist", Sort = 3, Enabled = true, Remark = "CRM 演示角色:数据范围=本机构(深圳分公司)" },
    ];
}
