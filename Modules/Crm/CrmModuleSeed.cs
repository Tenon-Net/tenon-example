using TenonAdmin.Services;
using TenonAdmin.SqlSugar;

namespace tenon_example.Modules.Crm;

/// <summary>
/// CRM 模块注册种子——固定 <see cref="ModuleId"/> 幂等锚点,供 P2 的角色/用户/菜单种子引用。
/// 只登记模块本身(门户侧边栏分区),不预置菜单树:业务模块的菜单通常经后台「菜单管理」UI 添加。
/// </summary>
public sealed class CrmModuleSeed : ISeedData<SysModule>
{
    /// <summary>CRM 模块固定主键(种子幂等锚点)</summary>
    public const long ModuleId = 1000;

    /// <summary>CRM 模块编码</summary>
    public const string ModuleCode = "crm";

    /// <summary>模块登记随内核/consumer 升级同步(允许改标题/图标等元数据)</summary>
    public bool SyncOnUpgrade => true;

    public IEnumerable<SysModule> HasData() =>
    [
        new SysModule
        {
            Id = ModuleId,
            Code = ModuleCode,
            Title = "客户管理",
            Icon = "lucide:users",
            DefaultRoute = "/crm/customer",
            ApiPrefix = "biz",
            Sort = 10,
            Enabled = true,
            Remark = "参考应用 CRM-lite 模块——多机构数据范围头条演示",
        },
    ];
}
