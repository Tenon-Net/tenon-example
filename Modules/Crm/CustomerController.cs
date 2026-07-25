using Microsoft.AspNetCore.Mvc;
using TenonAdmin.AspNetCore;
using TenonAdmin.Core;

namespace tenon_example.Modules.Crm;

/// <summary>
/// CRM 客户端点——与内置控制器同一管道(统一信封 / 鉴权 / 数据范围过滤)。
/// 全部 <c>[RolePermission]</c>:超管放行,普通用户需被授予对应路由权限码(权限码 = 规范化路由)。
/// </summary>
[ApiController]
[Route("api/v1/biz/customer")]
public class CustomerController(ICustomerService svc) : ControllerBase
{
    /// <summary>分页查询客户</summary>
    [HttpGet("page")]
    [RolePermission]
    public async Task<Result<PagedList<Customer>>> Page([FromQuery] CustomerPageInput input) =>
        Result<PagedList<Customer>>.Ok(await svc.PageAsync(input));

    /// <summary>按 Id 取单条客户</summary>
    [HttpGet("{id}")]
    [RolePermission]
    public async Task<Result<Customer>> Get(long id) =>
        Result<Customer>.Ok(await svc.GetAsync(id));

    /// <summary>新增客户</summary>
    [HttpPost("add")]
    [RolePermission]
    public async Task<Result<long>> Add(CustomerInput input) =>
        Result<long>.Ok(await svc.AddAsync(input));

    /// <summary>更新客户</summary>
    [HttpPut("{id}")]
    [RolePermission]
    public async Task<Result<bool>> Update(long id, CustomerInput input)
    {
        await svc.UpdateAsync(id, input);
        return Result<bool>.Ok(true);
    }

    /// <summary>删除客户</summary>
    [HttpDelete("{id}")]
    [RolePermission]
    public async Task<Result<bool>> Delete(long id)
    {
        await svc.DeleteAsync(id);
        return Result<bool>.Ok(true);
    }

    /// <summary>当前登录用户对客户列表生效的数据范围——结构化 DTO,前端据此拼 zh/en 文案</summary>
    [HttpGet("scope")]
    [RolePermission]
    public async Task<Result<CustomerScopeDto>> Scope() =>
        Result<CustomerScopeDto>.Ok(await svc.GetScopeAsync());
}
