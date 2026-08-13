using SqlSugar;
using TenonAdmin.Core;
using TenonAdmin.Services;
using TenonAdmin.SqlSugar;

namespace tenon_example.Modules.Crm;

/// <summary>
/// <see cref="ICustomerService"/> 默认实现——CRUD 全程不出现 <c>CreateOrgId</c> 或任何机构 <c>WHERE</c> 条件:
/// 读走 <c>AsQueryable()</c>,由内核全局过滤器按当前数据范围自动裁剪;改/删先 <c>GetByIdAsync</c>
/// (同样经范围过滤)校验可见性,仓储写路径另有越权兜底(双保险,§8)。
/// </summary>
public class CustomerService(IRepository<Customer> customers, IRepository<SysOrg> orgs, IDataScopeContext scopeContext) : ICustomerService
{
    /// <inheritdoc />
    public virtual async Task<PagedList<Customer>> PageAsync(CustomerPageInput input) =>
        await BuildListQuery(input)
            .ToPagedListAsync(input, q => q.OrderByDescending(c => c.Id));

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<Customer>> ExportAsync(CustomerPageInput input)
    {
        // 多取一条判断是否超过内核 Excel 默认 MaxExportRows(50000);这里用保守上限 +1。
        const int cap = 50_001;
        var list = await BuildListQuery(input)
            .OrderByDescending(c => c.Id)
            .Take(cap)
            .ToListAsync();
        AdminException.ThrowIf(list.Count >= cap, ErrorCode.ExportRowLimitExceeded);
        return list;
    }

    /// <summary>列表与导出共用的查询——名称模糊 + 全局数据范围过滤器。</summary>
    protected virtual ISugarQueryable<Customer> BuildListQuery(CustomerPageInput input) =>
        customers.AsQueryable()
            .WhereIF(!string.IsNullOrEmpty(input.Name), c => c.Name.Contains(input.Name!));

    /// <inheritdoc />
    public virtual async Task<Customer> GetAsync(long id)
    {
        var customer = await customers.GetByIdAsync(id);
        AdminException.ThrowIf(customer is null, (ErrorCode)BizErrorCode.CustomerNotFound);
        return customer!;
    }

    /// <inheritdoc />
    public virtual async Task<long> AddAsync(CustomerInput input)
    {
        var customer = new Customer
        {
            Name = input.Name,
            Contact = input.Contact,
            Phone = input.Phone,
            IntendedAmount = input.IntendedAmount,
            Status = input.Status,
        };
        await customers.InsertAsync(customer);
        return customer.Id;
    }

    /// <inheritdoc />
    public virtual async Task UpdateAsync(long id, CustomerInput input)
    {
        var customer = await GetAsync(id);
        customer.Name = input.Name;
        customer.Contact = input.Contact;
        customer.Phone = input.Phone;
        customer.IntendedAmount = input.IntendedAmount;
        customer.Status = input.Status;
        await customers.UpdateAsync(customer);
    }

    /// <inheritdoc />
    public virtual async Task DeleteAsync(long id)
    {
        await GetAsync(id);
        await customers.DeleteAsync(id);
    }

    /// <inheritdoc />
    public virtual async Task<CustomerScopeDto> GetScopeAsync() =>
        ComputeScope(
            // 0.6.0 起 IOrgService.ListAsync 会按当前范围裁剪(+祖先,QA08),缺掉的兄弟节点
            // 会让「非整棵子树」被误判成 OrgAndChildren。分类必须对照完整机构树。
            await orgs.AsQueryable().OrderBy(o => o.Sort).OrderBy(o => o.Id).ToListAsync(),
            scopeContext.Current);

    /// <summary>
    /// 纯函数,便于单测:把生效数据范围翻译成结构化语义。规则(§3 P3 锁定):不受限=全部;
    /// 受限时找出 <c>OrgIds</c> 中没有可见祖先的最小根——若其全部后代也在 <c>OrgIds</c> 中则是
    /// "根机构及以下",单机构且无下级是"机构",其余组合是"指定若干机构"。<c>IncludeSelf</c> 与
    /// <c>Kind</c> 正交,始终原样透出,由前端叠加"本人"语义。
    /// </summary>
    internal static CustomerScopeDto ComputeScope(IReadOnlyList<SysOrg> allOrgs, DataScopeResult scope)
    {
        if (scope.IsUnrestricted)
            return new CustomerScopeDto { Kind = CustomerScopeKind.All, VisibleOrgCount = allOrgs.Count, IncludeSelf = false };

        var orgIds = scope.OrgIds.ToHashSet();
        if (orgIds.Count == 0)
            return new CustomerScopeDto { Kind = CustomerScopeKind.None, VisibleOrgCount = 0, IncludeSelf = scope.IncludeSelf };

        var byId = allOrgs.ToDictionary(o => o.Id);

        bool HasVisibleAncestor(long id)
        {
            var current = byId.GetValueOrDefault(id);
            while (current is not null && current.ParentId != 0)
            {
                if (orgIds.Contains(current.ParentId)) return true;
                current = byId.GetValueOrDefault(current.ParentId);
            }
            return false;
        }

        var roots = orgIds.Where(id => !HasVisibleAncestor(id)).ToList();

        if (roots.Count == 1)
        {
            var root = roots[0];
            var descendants = Descendants(byId, root);
            if (descendants.Count > 0 && descendants.All(orgIds.Contains))
                return new CustomerScopeDto
                {
                    Kind = CustomerScopeKind.OrgAndChildren,
                    OrgName = byId[root].Name,
                    VisibleOrgCount = orgIds.Count,
                    IncludeSelf = scope.IncludeSelf,
                };

            if (orgIds.Count == 1)
                return new CustomerScopeDto
                {
                    Kind = CustomerScopeKind.Org,
                    OrgName = byId[root].Name,
                    VisibleOrgCount = 1,
                    IncludeSelf = scope.IncludeSelf,
                };
        }

        return new CustomerScopeDto { Kind = CustomerScopeKind.Specified, VisibleOrgCount = orgIds.Count, IncludeSelf = scope.IncludeSelf };
    }

    private static HashSet<long> Descendants(Dictionary<long, SysOrg> byId, long rootId)
    {
        var children = byId.Values.Where(o => o.ParentId == rootId).Select(o => o.Id).ToList();
        var result = new HashSet<long>(children);
        foreach (var child in children)
            result.UnionWith(Descendants(byId, child));
        return result;
    }
}
