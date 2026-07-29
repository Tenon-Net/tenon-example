using TenonAdmin.Services;
using TenonAdmin.SqlSugar;

namespace tenon_example.Modules.Crm.Seeds;

/// <summary>
/// CRM 客户管理页菜单节点——根级 Menu + 只读业务按钮 + 导入导出体验按钮。
/// 新增/更新/删除权限码仍不预授(演示三账号不可写);导入 commit 在 dry-run 下不落库。
/// template 端点是 [ActiveSession],无需按钮种子。
/// </summary>
public sealed class CrmMenuSeed : ISeedData<SysMenu>
{
    public const long CustomerPageMenuId = 1000;
    public const long CustomerPageButtonId = 1001;
    public const long CustomerDetailButtonId = 1002;
    public const long CustomerScopeButtonId = 1003;
    public const long CustomerImportPreviewButtonId = 1004;
    public const long CustomerImportValidateButtonId = 1005;
    public const long CustomerImportErrorReportButtonId = 1006;
    public const long CustomerImportCommitButtonId = 1007;
    public const long CustomerExportButtonId = 1008;

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
        new SysMenu { Id = CustomerImportPreviewButtonId, ParentId = CustomerPageMenuId, Type = MenuType.Button, Title = "客户-导入预览", Permission = "POST:/api/v1/biz/customer/import/preview", Sort = 4, Enabled = true },
        new SysMenu { Id = CustomerImportValidateButtonId, ParentId = CustomerPageMenuId, Type = MenuType.Button, Title = "客户-导入重验", Permission = "POST:/api/v1/biz/customer/import/validate", Sort = 5, Enabled = true },
        new SysMenu { Id = CustomerImportErrorReportButtonId, ParentId = CustomerPageMenuId, Type = MenuType.Button, Title = "客户-导入错误报告", Permission = "POST:/api/v1/biz/customer/import/error-report", Sort = 6, Enabled = true },
        new SysMenu { Id = CustomerImportCommitButtonId, ParentId = CustomerPageMenuId, Type = MenuType.Button, Title = "客户-导入提交", Permission = "POST:/api/v1/biz/customer/import/commit", Sort = 7, Enabled = true },
        new SysMenu { Id = CustomerExportButtonId, ParentId = CustomerPageMenuId, Type = MenuType.Button, Title = "客户-导出", Permission = "GET:/api/v1/biz/customer/export", Sort = 8, Enabled = true },
    ];
}
