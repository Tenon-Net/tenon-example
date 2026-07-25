using TenonAdmin.Services;
using TenonAdmin.SqlSugar;

namespace tenon_example.Modules.Crm.Seeds;

/// <summary>
/// CRM 演示机构树(§2 头条弹药)——集团总部 → 华南大区(深圳/广州/东莞)与华北大区(北京/天津)。
/// 客户只挂在 5 个分公司叶子机构上,大区/总部节点本身不直接挂客户,数据范围才有"及以下"可演。
/// </summary>
public sealed class CrmOrgSeed : ISeedData<SysOrg>
{
    public const long HeadquartersId = 1000;
    public const long SouthRegionId = 1001;
    public const long ShenzhenId = 1002;
    public const long GuangzhouId = 1003;
    public const long DongguanId = 1004;
    public const long NorthRegionId = 1005;
    public const long BeijingId = 1006;
    public const long TianjinId = 1007;

    public IEnumerable<SysOrg> HasData() =>
    [
        new SysOrg { Id = HeadquartersId, ParentId = 0, Name = "集团总部", Code = "crm_hq", Sort = 1, Enabled = true },
        new SysOrg { Id = SouthRegionId, ParentId = HeadquartersId, Name = "华南大区", Code = "crm_south", Sort = 1, Enabled = true },
        new SysOrg { Id = ShenzhenId, ParentId = SouthRegionId, Name = "深圳分公司", Code = "crm_sz", Sort = 1, Enabled = true },
        new SysOrg { Id = GuangzhouId, ParentId = SouthRegionId, Name = "广州分公司", Code = "crm_gz", Sort = 2, Enabled = true },
        new SysOrg { Id = DongguanId, ParentId = SouthRegionId, Name = "东莞分公司", Code = "crm_dg", Sort = 3, Enabled = true },
        new SysOrg { Id = NorthRegionId, ParentId = HeadquartersId, Name = "华北大区", Code = "crm_north", Sort = 2, Enabled = true },
        new SysOrg { Id = BeijingId, ParentId = NorthRegionId, Name = "北京分公司", Code = "crm_bj", Sort = 1, Enabled = true },
        new SysOrg { Id = TianjinId, ParentId = NorthRegionId, Name = "天津分公司", Code = "crm_tj", Sort = 2, Enabled = true },
    ];
}
