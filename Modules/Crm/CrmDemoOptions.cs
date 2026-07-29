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
}
