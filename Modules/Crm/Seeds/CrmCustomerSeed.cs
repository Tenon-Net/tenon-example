using TenonAdmin.SqlSugar;

namespace tenon_example.Modules.Crm.Seeds;

/// <summary>
/// CRM 演示客户数据(§2 头条弹药,最容易翻车的一步)——214 条客户分挂 5 个分公司叶子机构:
/// 深圳 42 / 广州 43 / 东莞 43(华南合计 128)+ 北京 43 / 天津 43(华北合计 86)。
/// 启动种子没有登录用户上下文,<b>不能靠运行期 AOP 回填</b>——本类显式给每行填 <c>CreateOrgId</c>/<c>CreateUserId</c>。
/// </summary>
public sealed class CrmCustomerSeed : ISeedData<Customer>
{
    /// <summary>客户种子固定 Id 起点(214 行占 1000-1213,消费者保留区间内)。</summary>
    private const long FirstId = 1000;

    /// <summary>创建人锚点——内置超级管理员(Id=1),种子行没有真实创建者,记系统身份。</summary>
    private const long SeedCreatorUserId = 1;

    private static readonly (long OrgId, string CityName, int Count)[] Branches =
    [
        (CrmOrgSeed.ShenzhenId, "深圳", 42),
        (CrmOrgSeed.GuangzhouId, "广州", 43),
        (CrmOrgSeed.DongguanId, "东莞", 43),
        (CrmOrgSeed.BeijingId, "北京", 43),
        (CrmOrgSeed.TianjinId, "天津", 43),
    ];

    public IEnumerable<Customer> HasData()
    {
        var id = FirstId;
        foreach (var (orgId, cityName, count) in Branches)
        {
            for (var i = 1; i <= count; i++)
            {
                yield return new Customer
                {
                    Id = id++,
                    Name = $"{cityName}客户{i:D3}",
                    Contact = $"联系人{i:D3}",
                    Phone = null,
                    IntendedAmount = 10_000m * ((i % 10) + 1),
                    Status = (CustomerStatus)(i % 4),
                    CreateOrgId = orgId,
                    CreateUserId = SeedCreatorUserId,
                };
            }
        }
    }
}
