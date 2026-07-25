using TenonAdmin.Core;

namespace tenon_example.Modules.Crm;

/// <summary>客户新增/编辑入参(增改共用)</summary>
public record CustomerInput
{
    public string Name { get; init; } = "";
    public string Contact { get; init; } = "";
    public string? Phone { get; init; }
    public decimal IntendedAmount { get; init; }
    public CustomerStatus Status { get; init; } = CustomerStatus.New;
}

/// <summary>客户分页查询入参:通用分页 + 名称模糊过滤</summary>
public record CustomerPageInput : PageInputBase
{
    /// <summary>客户名称(模糊匹配,可选)</summary>
    public string? Name { get; init; }
}

/// <summary>
/// 当前登录用户可见范围的语义分类。前端按 <c>Kind</c> + <c>IncludeSelf</c> 经 zh/en i18n 拼文案,
/// 后端只给结构化事实,不吐固定中文句子。
/// </summary>
public enum CustomerScopeKind
{
    /// <summary>不受限——看全部机构</summary>
    All,

    /// <summary>单一根机构及其全部下级(<see cref="CustomerScopeDto.OrgName"/> 为该根机构名)</summary>
    OrgAndChildren,

    /// <summary>单一机构,不含下级</summary>
    Org,

    /// <summary>其他组合(多个机构、或非整棵子树覆盖)</summary>
    Specified,

    /// <summary>无机构范围(通常与 <see cref="CustomerScopeDto.IncludeSelf"/> 搭配,表达"仅本人")</summary>
    None,
}

/// <summary>
/// 客户列表当前生效数据范围——结构化 DTO,不含任何语言文案。<c>IncludeSelf</c> 与 <c>Kind</c> 正交:
/// 为真时前端在 <c>Kind</c> 对应文案基础上叠加"本人"语义(<c>Kind=None</c> 时单独表达"仅本人")。
/// </summary>
public record CustomerScopeDto
{
    public required CustomerScopeKind Kind { get; init; }

    /// <summary>Kind 为 OrgAndChildren / Org 时的机构名称;其余情形为 null</summary>
    public string? OrgName { get; init; }

    /// <summary>可见机构数量(Kind=All 时为机构总数;Kind=Specified 时为实际可见机构数;其余为 0 或 1)</summary>
    public int VisibleOrgCount { get; init; }

    public bool IncludeSelf { get; init; }
}
