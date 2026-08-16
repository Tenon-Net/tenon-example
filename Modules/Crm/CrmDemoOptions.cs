namespace tenon_example.Modules.Crm;

/// <summary>
/// CRM 演示开关。公开 demo 默认 <see cref="ImportDryRun"/> = true:
/// 导入向导可完整走完(上传/预览/校验/提交),但 <c>CommitRowAsync</c> 不写库,避免脏数据。
/// </summary>
public sealed class CrmDemoOptions
{
    public const string SectionName = "CrmDemo";

    /// <summary>
    /// 为 true 时客户导入提交只跑校验与结果计数,不 Insert/Update。
    /// 本地 dogfood 真落库时在 appsettings.Development.json 设为 false。
    /// </summary>
    public bool ImportDryRun { get; set; } = true;

    /// <summary>
    /// 为 true 时拒绝一切写请求(见 <see cref="DemoReadOnlyFilter"/>),菜单与按钮照常渲染、点得动,只是不落库。
    /// 公开部署打开;本地默认关,四个账号的增删改查都是真的。
    /// 内核的 <c>TenonAdmin:DemoMode</c> 做不到这件事:它没有放行名单,会连导入向导的 POST 一起拦掉。
    /// </summary>
    public bool ReadOnly { get; set; }
}
