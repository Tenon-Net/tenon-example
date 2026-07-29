using TenonAdmin.Core;

namespace tenon_example.Modules.Crm;

/// <summary>
/// CRM 客户服务——标准 CRUD + 分页,外加当前数据范围的结构化说明。
/// 全程不写 <c>CreateOrgId</c> 或任何机构过滤条件:可见性完全交给内核全局过滤器 + 角色数据范围。
/// </summary>
public interface ICustomerService
{
    /// <summary>分页查询客户,按名称模糊过滤,按 Id 降序返回(新建的在前)。</summary>
    Task<PagedList<Customer>> PageAsync(CustomerPageInput input);

    /// <summary>
    /// 导出取数——与 <see cref="PageAsync"/> 同源过滤,但不走分页截断
    /// (PageAsync 的 MAX_SIZE=200 会静默少导,见 wire-import-export 坑 1)。
    /// </summary>
    Task<IReadOnlyList<Customer>> ExportAsync(CustomerPageInput input);

    /// <summary>按 Id 取单条,不在当前数据范围内或不存在均抛 <see cref="BizErrorCode.CustomerNotFound"/>。</summary>
    Task<Customer> GetAsync(long id);

    /// <summary>新增客户,返回新 Id(CreateOrgId/CreateUserId 由审计 AOP 从当前用户回填)。</summary>
    Task<long> AddAsync(CustomerInput input);

    /// <summary>更新客户;跨数据范围或不存在均抛 <see cref="BizErrorCode.CustomerNotFound"/>。</summary>
    Task UpdateAsync(long id, CustomerInput input);

    /// <summary>删除客户(软删);跨数据范围或不存在均抛 <see cref="BizErrorCode.CustomerNotFound"/>。</summary>
    Task DeleteAsync(long id);

    /// <summary>当前登录用户对客户列表生效的数据范围,结构化 DTO(不含文案)。</summary>
    Task<CustomerScopeDto> GetScopeAsync();
}
