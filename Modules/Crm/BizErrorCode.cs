namespace tenon_example.Modules.Crm;

/// <summary>
/// 消费方业务错误码。内核 <c>ErrorCode</c> 是不可扩展的枚举,消费方新增业务码只能强转 int
/// (<c>(TenonAdmin.Core.ErrorCode)BizErrorCode.CustomerNotFound</c>),集中在本类避免裸数字散落各处。
/// 从 60000 起步,内核分段(见 <c>ErrorCode</c> 头注释)止步 50999,两边不会撞号。
/// 未标注 <c>MsgKey</c> 的码回退为 <c>error.code.{n}</c>,前端语言包按此键补文案即可。
/// </summary>
public static class BizErrorCode
{
    /// <summary>客户不存在或不在当前数据范围内</summary>
    public const int CustomerNotFound = 60001;
}
