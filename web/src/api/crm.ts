// CRM 业务模块自己的 API 域——从 ./index 借 unwrap/pageParams/toPage,不碰上游自留地 api/index.ts。
// 路径走 `as any` 客户端:业务路由不在上游 schema 的 paths 联合里,gen:api 后仍可保持此写法避免 merge 冲突。
import { client } from './client'
import { pageParams, toPage, unwrap, ApiError } from './index'
import type { Customer, CustomerInput, CustomerScope } from '@/types/crm'
import type {
  DuplicateStrategy,
  ImportCommitResult,
  ImportPreview,
  ImportRow,
} from '@/types/api'

/** 客户导入提交结果 = 内核 ImportCommitResult + dryRun 标记。 */
export interface CustomerImportCommitResult extends ImportCommitResult {
  dryRun: boolean
}

/**
 * 下载 xlsx:parseAs blob + 手查 ok(与 userApi 同型;helpers 在 index 未导出,这里自给)。
 */
async function unwrapDownload(res: { data?: unknown; error?: unknown; response: Response }): Promise<Blob> {
  const { data, error, response } = res
  if (!response.ok) {
    const env = (error ?? {}) as { code?: number; msgKey?: string; args?: Record<string, unknown>; message?: string }
    if (typeof env.code === 'number') throw new ApiError(env.code, env.msgKey, env.args, env.message)
    throw new ApiError(response.status, undefined, undefined, response.statusText)
  }
  const blob = data as Blob
  const ct = response.headers.get('content-type') ?? ''
  if (ct.includes('spreadsheet') || ct.includes('octet-stream')) return blob
  if (ct.includes('json') || ct.includes('text/plain')) {
    const env = JSON.parse(await blob.text()) as { code?: number; msgKey?: string; args?: Record<string, unknown>; message?: string }
    if (typeof env.code === 'number' && env.code !== 0) {
      throw new ApiError(env.code, env.msgKey, env.args, env.message)
    }
  }
  return blob
}

function normalizePreview(raw: ImportPreview): ImportPreview {
  return {
    ...raw,
    total: Number(raw.total ?? 0),
    errorRows: Number(raw.errorRows ?? 0),
    headers: raw.headers ?? [],
    columns: raw.columns ?? [],
    rows: (raw.rows ?? []).map((r) => ({
      ...r,
      index: Number(r.index ?? 0),
      cells: r.cells ?? {},
      errors: r.errors ?? [],
    })),
    columnErrors: raw.columnErrors ?? [],
    mapping: raw.mapping ?? {},
  }
}

function normalizeCommit(raw: CustomerImportCommitResult): CustomerImportCommitResult {
  return {
    total: Number(raw.total ?? 0),
    inserted: Number(raw.inserted ?? 0),
    updated: Number(raw.updated ?? 0),
    skipped: Number(raw.skipped ?? 0),
    failed: Number(raw.failed ?? 0),
    failures: raw.failures ?? [],
    dryRun: Boolean(raw.dryRun),
  }
}

function toWireRows(rows: ImportRow[]) {
  return rows.map((r) => ({
    index: r.index,
    cells: r.cells,
    errors: r.errors,
  }))
}

// openapi schema 在 gen:api 前可能缺 CRM 导入导出路径;用 as never 绕开 paths 收窄,运行期仍走真实路由。
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const c = client as any

export const customerApi = {
  page: (params: { page: number; pageSize: number; name?: string; sortField?: string; sortOrder?: string }) =>
    c
      .GET('/api/v1/biz/customer/page', {
        params: {
          query: {
            ...pageParams(params),
            Name: params.name,
            SortField: params.sortField,
            SortOrder: params.sortOrder,
          },
        },
      })
      .then((r: Parameters<typeof toPage>[0]) => toPage<Customer>(r)),
  add: (body: CustomerInput) =>
    c.POST('/api/v1/biz/customer/add', { body }).then((r: Parameters<typeof unwrap>[0]) => unwrap<number>(r)),
  update: (id: number, body: CustomerInput) =>
    c
      .PUT('/api/v1/biz/customer/{id}', { params: { path: { id } }, body })
      .then((r: Parameters<typeof unwrap>[0]) => unwrap<boolean>(r)),
  remove: (id: number) =>
    c
      .DELETE('/api/v1/biz/customer/{id}', { params: { path: { id } } })
      .then((r: Parameters<typeof unwrap>[0]) => unwrap<boolean>(r)),
  /** 当前登录用户对客户列表生效的数据范围——结构化 DTO,列表页顶部据此拼当前范围文案(§2 头条)。 */
  scope: () =>
    c.GET('/api/v1/biz/customer/scope', {}).then((r: Parameters<typeof unwrap>[0]) => unwrap<CustomerScope>(r)),

  // ── 导入 / 导出 ──

  importTemplate: () =>
    c.GET('/api/v1/biz/customer/import/template', { parseAs: 'blob' }).then((r: Parameters<typeof unwrapDownload>[0]) =>
      unwrapDownload(r),
    ),

  importPreview: (file: File, mapping?: Record<string, string>) =>
    c
      .POST('/api/v1/biz/customer/import/preview', {
        body: {
          file: file as unknown as string,
          mapping: mapping ? JSON.stringify(mapping) : undefined,
        },
        bodySerializer: (body: { file: File; mapping?: string }) => {
          const fd = new FormData()
          fd.append('file', body.file)
          if (body.mapping) fd.append('mapping', body.mapping)
          return fd
        },
      })
      .then((r: Parameters<typeof unwrap>[0]) => normalizePreview(unwrap<ImportPreview>(r))),

  importValidate: (rows: ImportRow[]) =>
    c
      .POST('/api/v1/biz/customer/import/validate', { body: { rows: toWireRows(rows) } })
      .then((r: Parameters<typeof unwrap>[0]) => normalizePreview(unwrap<ImportPreview>(r))),

  importErrorReport: (rows: ImportRow[]) =>
    c
      .POST('/api/v1/biz/customer/import/error-report', {
        body: { rows: toWireRows(rows) },
        parseAs: 'blob',
      })
      .then((r: Parameters<typeof unwrapDownload>[0]) => unwrapDownload(r)),

  importCommit: (rows: ImportRow[], strategy: DuplicateStrategy = 0) =>
    c
      .POST('/api/v1/biz/customer/import/commit', {
        body: { rows: toWireRows(rows), strategy },
      })
      .then((r: Parameters<typeof unwrap>[0]) => normalizeCommit(unwrap<CustomerImportCommitResult>(r))),

  export: (params: { name?: string; sortField?: string; sortOrder?: string; columns?: string }) =>
    c
      .GET('/api/v1/biz/customer/export', {
        params: {
          query: {
            Name: params.name,
            SortField: params.sortField,
            SortOrder: params.sortOrder,
            columns: params.columns,
          },
        },
        parseAs: 'blob',
      })
      .then((r: Parameters<typeof unwrapDownload>[0]) => unwrapDownload(r)),
}
