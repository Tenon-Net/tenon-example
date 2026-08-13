using TenonAdmin.Services;
using TenonAdmin.SqlSugar;

namespace tenon_example.Modules.Crm.Seeds;

/// <summary>
/// 把内核自带「系统」模块(<c>DefaultModuleSeed.BUILTIN_MODULE_ID</c>=1:组织/用户/角色/菜单/字典/
/// 配置/日志/文件全树)整棵授权给总部管理员——这个 demo 的"管理员账号"故事:一个真实 RBAC 角色能看到
/// 内核出厂即带的全部后台能力,不是只有 CRM 一个菜单。华南区域经理/深圳专员保持 CRM-only(业务角色人设),
/// 不在此列。写按钮(新增/编辑/删除)也一并授权——DemoMode 会在后端统一拒绝写请求,展示后拒绝是接受的取舍。
/// <para>Id 列表对应内核 backend/src/TenonAdmin.Services/Seed/DefaultMenuSeed.cs 当前版本;
/// 内核后续新增系统菜单不会自动补进来,升级内核版本时需要跟着补。</para>
/// </summary>
public sealed class CrmHqAdminSystemMenuSeed : ISeedData<SysRoleMenu>
{
    /// <summary>连接表:唯一索引在 (RoleId, MenuId),按此判存(同内核角色菜单授权语义)。</summary>
    public string[] DedupColumns => [nameof(SysRoleMenu.RoleId), nameof(SysRoleMenu.MenuId)];

    private static readonly long[] SystemMenuIds =
    [
        108, // 工作台
        10, 70, 13, 71, 72, 73, 98, // 组织管理 › 机构管理
        74, 14, 75, 76, 77, // 组织管理 › 岗位管理
        15, 11, 12, 50, 51, 52, 83, 53, 54, // 组织管理 › 用户管理
        87, 88, 89, 91, 92, 93, 94, 95, 96, 3, 4, 114, 115, // 组织管理 › 角色管理
        20, 55, 24, 25, 57, 58, 59, 97, // 系统运维 › 系统配置
        60, 21, 22, 23, 61, 62, 63, 64, 65, 82, 84, 85, // 系统运维 › 字典管理
        40, 41, 42, 43, 44, 107, // 系统运维 › 菜单管理
        45, 46, 47, 48, 49, // 系统运维 › 模块管理
        100, 101, 102, 103, // 系统运维 › 消息通知
        2, // 系统运维 › 连通性探针
        110, 111, 112, 113, // 系统运维 › 回收站
        119, 120, // 系统运维 › 服务器监控
        121, 122, 123, 124, 125, // 系统运维 › 缓存管理
        90, 68, 8, 69, // 日志审计 › 登录日志
        66, 7, 67, // 日志审计 › 操作日志
        116, 117, 118, // 日志审计 › 异常日志
        81, 5, 6, // 日志审计 › 在线会话
        30, 78, 31, 32, 79, 80, 86, 104, 105, 106, // 文件管理 › 文件管理
        // 以下为 0.6.0 新增,只能追加在末尾:本种子按数组下标分配主键 1100+,
        // 插到中间会让存量库里已写入的 SysRoleMenu.Id 全部错位。
        126, 127, 128, 129, 130, // 用户-导入/导出
        157, 151, 152, 153, 156, // 配置-外部登录 / 高敏 / 清 MFA(154/155 内核默认停用,不授)
        131, // 操作日志-导出
        147, 132, 133, 134, 135, 136, 137, 138, 139, 146, 140, 141, 142, 143, 144, 145, // 任务调度
    ];

    public IEnumerable<SysRoleMenu> HasData()
    {
        // 与 CrmRoleMenuSeed 的 1000-1008 错开,避免 SysRoleMenu 主键撞号。
        long id = 1100;
        foreach (var menuId in SystemMenuIds)
            yield return new SysRoleMenu { Id = id++, RoleId = CrmRoleSeed.HqAdminRoleId, MenuId = menuId };
    }
}
