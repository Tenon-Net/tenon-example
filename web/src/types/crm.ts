/** CRM 客户行(后端 tenon_example.Modules.Crm.Customer) */
export interface Customer {
  id: number
  name: string
  contact: string
  phone?: string | null
  intendedAmount: number
  status: CustomerStatus
  createTime?: string
}

/** 客户跟进状态(后端 CustomerStatus 枚举,存库为 int) */
export enum CustomerStatus {
  New = 0,
  Following = 1,
  Won = 2,
  Lost = 3,
}

/** 客户新增/编辑入参(后端 CustomerInput) */
export interface CustomerInput {
  name: string
  contact: string
  phone?: string | null
  intendedAmount: number
  status: CustomerStatus
}

/** 数据范围语义分类(后端 CustomerScopeKind 枚举) */
export enum CustomerScopeKind {
  All = 0,
  OrgAndChildren = 1,
  Org = 2,
  Specified = 3,
  None = 4,
}

/** 当前登录用户对客户列表生效的数据范围(后端 CustomerScopeDto,结构化,不含文案) */
export interface CustomerScope {
  kind: CustomerScopeKind
  orgName?: string | null
  visibleOrgCount: number
  includeSelf: boolean
}
