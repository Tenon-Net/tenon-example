using TenonAdmin.Core;

namespace tenon_example.Modules.Crm;

/// <summary>客户导出档案——只声明列;取数走 <see cref="ICustomerService.ExportAsync"/>,与列表同源过滤器。</summary>
public class CustomerExportProfile : IExportProfile
{
    public virtual string Code => "biz-customer";

    public virtual IReadOnlyList<ExportColumn> Columns { get; } =
    [
        new() { Key = "Name", Title = "客户名称", Width = 24 },
        new() { Key = "Contact", Title = "联系人", Width = 14 },
        new() { Key = "Phone", Title = "联系电话", Width = 14 },
        new() { Key = "IntendedAmount", Title = "意向金额", Width = 12 },
        new() { Key = "Status", Title = "跟进状态", Width = 12 },
        new() { Key = "CreateTime", Title = "创建时间", Width = 18, DefaultSelected = false },
    ];
}
