using System.Globalization;
using Microsoft.Extensions.Options;
using TenonAdmin.Core;
using TenonAdmin.SqlSugar;

namespace tenon_example.Modules.Crm;

/// <summary>
/// 客户导入档案。业务键 = 客户名称;状态接受中文标签或枚举名/数字。
/// <see cref="CrmDemoOptions.ImportDryRun"/> 为 true 时 <see cref="CommitRowAsync"/> 空操作——
/// Runner 仍会重校验并累计 Inserted/Updated,前端提示「未写入」。
/// </summary>
public class CustomerImportProfile(
    ICustomerService customers,
    IRepository<Customer> repo,
    IOptions<CrmDemoOptions> demoOptions) : IImportProfile
{
    public virtual string Code => "biz-customer";

    public virtual IReadOnlyList<string> BusinessKeys { get; } = ["Name"];

    public virtual IReadOnlyList<ImportColumn> Columns { get; } =
    [
        new() { Key = "Name", Title = "客户名称", Required = true, Width = 24, Hint = "唯一客户名称(业务键)" },
        new() { Key = "Contact", Title = "联系人", Required = true, Width = 14 },
        new() { Key = "Phone", Title = "联系电话", Width = 14 },
        new() { Key = "IntendedAmount", Title = "意向金额", Width = 12, Hint = "数字,可留空(=0)" },
        new()
        {
            Key = "Status", Title = "跟进状态", Width = 12,
            Hint = "新增 / 跟进中 / 已成交 / 已流失(或 New/Following/Won/Lost)",
        },
    ];

    public virtual Task<IReadOnlyList<CellError>> ValidateRowAsync(
        ImportRow row, CancellationToken cancellationToken = default)
    {
        var errors = new List<CellError>();

        if (Cell(row, "IntendedAmount") is { Length: > 0 } amountRaw
            && !TryParseAmount(amountRaw, out _))
            errors.Add(new CellError("IntendedAmount", ErrorCode.ImportCellFormatInvalid));

        if (Cell(row, "Status") is { Length: > 0 } statusRaw
            && ParseStatus(statusRaw) is null)
            errors.Add(new CellError("Status", ErrorCode.ImportCellFormatInvalid));

        return Task.FromResult<IReadOnlyList<CellError>>(errors);
    }

    public virtual async Task<IReadOnlySet<string>> FindExistingKeysAsync(
        IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default)
    {
        if (keys.Count == 0) return new HashSet<string>();
        var list = keys.ToList();
        // 可见范围内的重名;数据范围过滤器仍生效,不 ClearFilter。
        var existing = await repo.AsQueryable()
            .Where(c => list.Contains(c.Name))
            .Select(c => c.Name)
            .ToListAsync();
        return existing.ToHashSet(StringComparer.Ordinal);
    }

    public virtual async Task CommitRowAsync(
        ImportRow row, bool overwrite, CancellationToken cancellationToken = default)
    {
        // 演示 dry-run:校验已由 Runner 跑完,这里不写库。
        if (demoOptions.Value.ImportDryRun)
            return;

        var name = Cell(row, "Name")?.Trim()
            ?? throw new AdminException(ErrorCode.ImportCellRequired);
        var contact = Cell(row, "Contact")?.Trim()
            ?? throw new AdminException(ErrorCode.ImportCellRequired);
        var phone = Cell(row, "Phone")?.Trim();
        TryParseAmount(Cell(row, "IntendedAmount"), out var amount);
        var status = ParseStatus(Cell(row, "Status")) ?? CustomerStatus.New;

        var input = new CustomerInput
        {
            Name = name,
            Contact = contact,
            Phone = string.IsNullOrEmpty(phone) ? null : phone,
            IntendedAmount = amount,
            Status = status,
        };

        if (overwrite)
        {
            var entity = await repo.AsQueryable()
                .Where(c => c.Name == name)
                .FirstAsync();
            await customers.UpdateAsync(entity.Id, input);
        }
        else
        {
            await customers.AddAsync(input);
        }
    }

    private static string? Cell(ImportRow row, string key) =>
        row.Cells.TryGetValue(key, out var v) ? v : null;

    internal static bool TryParseAmount(string? raw, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(raw)) return true;
        return decimal.TryParse(raw.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out amount)
            || decimal.TryParse(raw.Trim(), NumberStyles.Number, CultureInfo.GetCultureInfo("zh-CN"), out amount);
    }

    internal static CustomerStatus? ParseStatus(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return CustomerStatus.New;
        var s = raw.Trim();
        if (int.TryParse(s, out var n) && Enum.IsDefined(typeof(CustomerStatus), n))
            return (CustomerStatus)n;
        return s.ToLowerInvariant() switch
        {
            "new" or "新增" or "新" => CustomerStatus.New,
            "following" or "跟进中" or "跟进" => CustomerStatus.Following,
            "won" or "已成交" or "成交" => CustomerStatus.Won,
            "lost" or "已流失" or "流失" => CustomerStatus.Lost,
            _ => Enum.TryParse<CustomerStatus>(s, ignoreCase: true, out var e) ? e : null,
        };
    }
}
