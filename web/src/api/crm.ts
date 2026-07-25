// CRM 业务模块自己的 API 域——从 ./index 借 unwrap/pageParams/toPage,不碰上游自留地 api/index.ts。
import { client } from './client'
import { pageParams, toPage, unwrap } from './index'
import type { Customer, CustomerInput, CustomerScope } from '@/types/crm'

export const customerApi = {
  page: (params: { page: number; pageSize: number; name?: string; sortField?: string; sortOrder?: string }) =>
    client
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
      .then((r) => toPage<Customer>(r)),
  add: (body: CustomerInput) =>
    client.POST('/api/v1/biz/customer/add', { body }).then((r) => unwrap<number>(r)),
  update: (id: number, body: CustomerInput) =>
    client.PUT('/api/v1/biz/customer/{id}', { params: { path: { id } }, body }).then((r) => unwrap<boolean>(r)),
  remove: (id: number) =>
    client.DELETE('/api/v1/biz/customer/{id}', { params: { path: { id } } }).then((r) => unwrap<boolean>(r)),
  /** 当前登录用户对客户列表生效的数据范围——结构化 DTO,列表页顶部据此拼当前范围文案(§2 头条)。 */
  scope: () =>
    client.GET('/api/v1/biz/customer/scope', {}).then((r) => unwrap<CustomerScope>(r)),
}
