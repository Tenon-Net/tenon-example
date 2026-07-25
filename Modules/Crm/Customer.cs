using SqlSugar;
using TenonAdmin.SqlSugar;

namespace tenon_example.Modules.Crm;

/// <summary>
/// CRM 客户——本仓的头条演示实体。继承 <see cref="DataEntity"/> 获得 <c>CreateOrgId</c> 数据范围锚点:
/// <see cref="CustomerService"/> 全程零过滤代码,同一查询按登录身份返回不同行,由内核全局过滤器自动完成。
/// </summary>
[SugarTable("biz_crm_customer", TableDescription = "CRM 客户")]
public class Customer : DataEntity
{
    [SugarColumn(Length = 128, ColumnDescription = "客户名称")]
    public string Name { get; set; } = "";

    [SugarColumn(Length = 64, ColumnDescription = "联系人")]
    public string Contact { get; set; } = "";

    [SugarColumn(Length = 32, IsNullable = true, ColumnDescription = "联系电话")]
    public string? Phone { get; set; }

    [SugarColumn(DecimalDigits = 2, ColumnDescription = "意向金额")]
    public decimal IntendedAmount { get; set; }

    [SugarColumn(ColumnDescription = "跟进状态")]
    public CustomerStatus Status { get; set; } = CustomerStatus.New;
}

/// <summary>客户跟进状态</summary>
public enum CustomerStatus
{
    /// <summary>新增,未跟进</summary>
    New = 0,

    /// <summary>跟进中</summary>
    Following = 1,

    /// <summary>已成交</summary>
    Won = 2,

    /// <summary>已流失</summary>
    Lost = 3,
}
