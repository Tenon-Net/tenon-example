using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using TenonAdmin.Core;
using tenon_example.Modules.Crm;

namespace tenon_example.Tests;

/// <summary>公开演示的写入闸门:读放行、导入放行、其余写请求 41002。</summary>
public class DemoReadOnlyFilterTests
{
    private static AuthorizationFilterContext Run(string method, string path)
    {
        var http = new DefaultHttpContext();
        http.Request.Method = method;
        http.Request.Path = path;

        var context = new AuthorizationFilterContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor()), []);
        new DemoReadOnlyFilter().OnAuthorization(context);
        return context;
    }

    [Theory]
    [InlineData("GET", "/api/v1/biz/customer/page")]
    [InlineData("HEAD", "/api/v1/sys/user/page")]
    [InlineData("OPTIONS", "/api/v1/sys/user")]
    [InlineData("POST", "/api/v1/auth/login")]
    [InlineData("POST", "/api/v1/biz/customer/import/preview")]
    [InlineData("POST", "/api/v1/biz/customer/import/commit")]
    public void Allows_reads_auth_and_import(string method, string path)
    {
        Assert.Null(Run(method, path).Result);
    }

    [Theory]
    [InlineData("POST", "/api/v1/sys/user")]          // 建用户
    [InlineData("DELETE", "/api/v1/sys/user/1")]      // 删用户
    [InlineData("DELETE", "/api/v1/sys/log/login")]   // 清日志
    [InlineData("POST", "/api/v1/sys/file/upload")]   // 传文件
    [InlineData("PUT", "/api/v1/biz/customer/1")]     // 改客户
    [InlineData("POST", "/api/v1/biz/customer/add")]  // 建客户
    public void Rejects_every_other_write(string method, string path)
    {
        var result = Assert.IsType<ObjectResult>(Run(method, path).Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        var body = Assert.IsType<Result<object>>(result.Value);
        Assert.Equal((int)ErrorCode.DemoModeReadOnly, body.Code);
    }

    /// <summary>前缀匹配必须按段,否则 /api/v1/authx 之类的路径会被顺带放行。</summary>
    [Fact]
    public void Prefix_match_is_segment_based()
    {
        var result = Assert.IsType<ObjectResult>(Run("POST", "/api/v1/authx/anything").Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }
}
