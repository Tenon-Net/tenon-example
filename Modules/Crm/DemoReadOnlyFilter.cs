using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TenonAdmin.Core;

namespace tenon_example.Modules.Crm;

/// <summary>
/// 公开演示的写入闸门:除认证与客户导入外,一切非 GET 请求返回 403 / 41002。
/// 权限和菜单不动,所以按钮照常渲染、点得动,点下去拿到的是「演示环境只读」而不是脏数据。
/// 超管也拦——它绕过的是 [RolePermission],不是这里。
/// 仅在 <see cref="CrmDemoOptions.ReadOnly"/> 为 true 时注册。
/// </summary>
public sealed class DemoReadOnlyFilter : IAuthorizationFilter
{
    /// <summary>
    /// 放行的写路径前缀。导入四个接口(preview/validate/error-report/commit)本身不落库:
    /// 前三个只做解析与校验,commit 受 <see cref="CrmDemoOptions.ImportDryRun"/> 保护。
    /// 两个开关必须同时打开,少一个导入就会真的写进去。
    /// </summary>
    private static readonly string[] s_allowedWritePrefixes =
    [
        "/api/v1/auth",
        "/api/v1/biz/customer/import",
    ];

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;

        if (HttpMethods.IsGet(request.Method)
            || HttpMethods.IsHead(request.Method)
            || HttpMethods.IsOptions(request.Method))
            return;

        foreach (var prefix in s_allowedWritePrefixes)
        {
            if (request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
                return;
        }

        context.Result = new ObjectResult(Result<object>.Fail(ErrorCode.DemoModeReadOnly))
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };
    }
}
