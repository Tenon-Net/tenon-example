using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TenonAdmin.AspNetCore;
using TenonAdmin.Core;

namespace tenon_example.Modules.Crm;

/// <summary>
/// CRM 客户端点——与内置控制器同一管道(统一信封 / 鉴权 / 数据范围过滤)。
/// 导入六端点形状照 UserController;公开 demo 下 commit 走 dry-run(见 <see cref="CrmDemoOptions"/>)。
/// </summary>
[ApiController]
[Route("api/v1/biz/customer")]
public class CustomerController(
    ICustomerService svc,
    IImportRunner importRunner,
    IExcelTemplateBuilder templates,
    IExcelWriter writer,
    CustomerImportProfile importProfile,
    CustomerExportProfile exportProfile,
    IOptions<CrmDemoOptions> demoOptions,
    AdminExcelOptions excel) : ControllerBase
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

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

    // ── 导入 / 导出 ──

    /// <summary>下载导入模板。[ActiveSession]——只泄露列名,无需独立权限节点。</summary>
    [HttpGet("import/template")]
    [ActiveSession]
    public async Task<IActionResult> ImportTemplate(CancellationToken cancellationToken)
    {
        var stream = await templates.BuildAsync(new TemplateSpec
        {
            SheetName = "数据",
            Columns = importProfile.Columns,
        }, cancellationToken);
        return File(stream, XlsxContentType, "客户导入模板.xlsx");
    }

    /// <summary>上传 xlsx 预览校验。</summary>
    [HttpPost("import/preview")]
    [RolePermission]
    [RequestSizeLimit(32 * 1024 * 1024)]
    public async Task<Result<ImportPreview>> ImportPreview(
        IFormFile file,
        [FromForm] string? mapping,
        CancellationToken cancellationToken)
    {
        ValidateImportFile(file);
        IReadOnlyDictionary<string, string>? map = null;
        if (!string.IsNullOrWhiteSpace(mapping))
        {
            map = JsonSerializer.Deserialize<Dictionary<string, string>>(mapping)
                  ?? new Dictionary<string, string>();
        }

        await using var stream = file.OpenReadStream();
        await using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;

        var preview = await importRunner.PreviewAsync(ms, map, importProfile, cancellationToken);
        return Result<ImportPreview>.Ok(preview);
    }

    /// <summary>对前端改过的行重新校验。</summary>
    [HttpPost("import/validate")]
    [RolePermission]
    public async Task<Result<ImportPreview>> ImportValidate(
        ImportRowsInput input, CancellationToken cancellationToken)
    {
        var preview = await importRunner.ValidateAsync(input.Rows, importProfile, cancellationToken);
        return Result<ImportPreview>.Ok(preview);
    }

    /// <summary>下载错误报告 xlsx。</summary>
    [HttpPost("import/error-report")]
    [RolePermission]
    public async Task<IActionResult> ImportErrorReport(
        ImportRowsInput input, CancellationToken cancellationToken)
    {
        var cols = importProfile.Columns
            .Select(c => new ExportColumn { Key = c.Key, Title = c.Title, Width = c.Width })
            .Append(new ExportColumn { Key = "_errors", Title = "错误原因", Width = 40 })
            .ToList();

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var row in input.Rows)
        {
            var cells = new Dictionary<string, object?>();
            foreach (var col in importProfile.Columns)
                cells[col.Key] = row.Cells.TryGetValue(col.Key, out var v) ? v : null;
            cells["_errors"] = string.Join("; ",
                row.Errors.Select(e => $"{e.ColumnKey}:{e.Code}({(int)e.Code})"));
            rows.Add(cells);
        }

        var stream = await writer.WriteAsync(new ExportSheet
        {
            SheetName = "错误报告",
            Columns = cols,
            Rows = rows,
        }, cancellationToken);

        return File(stream, XlsxContentType, "客户导入错误报告.xlsx");
    }

    /// <summary>
    /// 提交导入。公开 demo 下 <see cref="CrmDemoOptions.ImportDryRun"/> 为 true:
    /// 服务端仍重校验并返回 Inserted/Updated 计数,但不写库。
    /// 响应信封的 <c>msgKey</c> 在 dry-run 时带自定义提示键(前端据此改文案)。
    /// </summary>
    [HttpPost("import/commit")]
    [RolePermission]
    [OperationLog("导入客户")]
    public async Task<Result<CustomerImportCommitResult>> ImportCommit(
        ImportCommitInput input, CancellationToken cancellationToken)
    {
        var result = await importRunner.CommitAsync(
            input.Rows, importProfile, input.Strategy, cancellationToken);
        return Result<CustomerImportCommitResult>.Ok(new CustomerImportCommitResult
        {
            Total = result.Total,
            Inserted = result.Inserted,
            Updated = result.Updated,
            Skipped = result.Skipped,
            Failed = result.Failed,
            Failures = result.Failures,
            DryRun = demoOptions.Value.ImportDryRun,
        });
    }

    /// <summary>导出客户 xlsx。筛选与列表同源 + columns 逗号分隔列 Key。</summary>
    [HttpGet("export")]
    [RolePermission]
    [OperationLog("导出客户")]
    public async Task<IActionResult> Export(
        [FromQuery] CustomerPageInput input,
        [FromQuery] string? columns,
        CancellationToken cancellationToken)
    {
        var selected = ResolveExportColumns(exportProfile, columns);
        var items = await svc.ExportAsync(input);

        var rows = items.Select(c =>
        {
            var cells = new Dictionary<string, object?>();
            foreach (var col in selected)
                cells[col.Key] = GetCustomerCell(c, col.Key);
            return (IReadOnlyDictionary<string, object?>)cells;
        }).ToList();

        var stream = await writer.WriteAsync(new ExportSheet
        {
            SheetName = "客户",
            Columns = selected,
            Rows = rows,
        }, cancellationToken);

        return File(stream, XlsxContentType, "客户导出.xlsx");
    }

    private void ValidateImportFile(IFormFile? file)
    {
        AdminException.ThrowIf(file is null || file.Length <= 0, ErrorCode.FileEmpty);
        var ext = Path.GetExtension(file!.FileName);
        AdminException.ThrowIf(
            !string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase),
            ErrorCode.FileExtNotAllowed,
            new Dictionary<string, object?> { ["ext"] = ext });
        var maxBytes = (long)excel.MaxImportFileSizeMb * 1024 * 1024;
        AdminException.ThrowIf(file.Length > maxBytes, ErrorCode.FileTooLarge,
            new Dictionary<string, object?> { ["maxSizeMb"] = excel.MaxImportFileSizeMb });
    }

    internal static IReadOnlyList<ExportColumn> ResolveExportColumns(IExportProfile profile, string? columnsCsv)
    {
        if (string.IsNullOrWhiteSpace(columnsCsv))
            return profile.Columns.Where(c => c.DefaultSelected).ToList();

        var keys = columnsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var byKey = profile.Columns.ToDictionary(c => c.Key, StringComparer.OrdinalIgnoreCase);
        var list = new List<ExportColumn>(keys.Length);
        foreach (var key in keys)
        {
            AdminException.ThrowIf(!byKey.TryGetValue(key, out var col), ErrorCode.ExportColumnInvalid,
                new Dictionary<string, object?> { ["column"] = key });
            list.Add(col!);
        }
        return list;
    }

    private static object? GetCustomerCell(Customer c, string key) => key switch
    {
        "Name" => c.Name,
        "Contact" => c.Contact,
        "Phone" => c.Phone,
        "IntendedAmount" => c.IntendedAmount,
        "Status" => StatusLabel(c.Status),
        "CreateTime" => c.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
        _ => null,
    };

    private static string StatusLabel(CustomerStatus s) => s switch
    {
        CustomerStatus.New => "新增",
        CustomerStatus.Following => "跟进中",
        CustomerStatus.Won => "已成交",
        CustomerStatus.Lost => "已流失",
        _ => s.ToString(),
    };
}

/// <summary>导入提交结果 + dry-run 标记(扩展内核 <see cref="ImportCommitResult"/>)。</summary>
public sealed class CustomerImportCommitResult
{
    public int Total { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public IReadOnlyList<ImportRow> Failures { get; set; } = [];
    /// <summary>true = 未写库,计数是「若提交会怎样」。</summary>
    public bool DryRun { get; set; }
}
