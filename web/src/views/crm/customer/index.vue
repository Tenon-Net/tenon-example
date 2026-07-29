<script setup lang="ts">
import { h, onMounted, reactive, ref } from 'vue'
import {
  NAlert, NButton, NForm, NFormItem, NInput, NInputNumber, NPopconfirm, NSelect, NSpace, NTag,
  useMessage, type FormInst, type FormRules,
} from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { ProTable, type ProTableColumn, type ProTableInst } from 'tenon-naive-pro-table'
import AppIcon from '@/components/AppIcon.vue'
import FormContainer from '@/components/FormContainer/index.vue'
import ImportWizard, { type ImportWizardApi } from '@/components/ImportWizard/index.vue'
import ExportColumnsModal from '@/components/ExportColumnsModal/index.vue'
import { useConfirm } from '@/composables/useConfirm'
import { useAuthStore } from '@/stores/auth'
import { customerApi } from '@/api/crm'
import { translateError } from '@/utils/error'
import { triggerBlobDownload } from '@/utils/download'
import type { ExportColumnDef } from '@/types/api'
import { CustomerScopeKind, CustomerStatus, type Customer, type CustomerInput, type CustomerScope } from '@/types/crm'

const { t } = useI18n()
const message = useMessage()
const { run } = useConfirm()
const auth = useAuthStore()
const tableRef = ref<ProTableInst<Customer>>()

// ── 数据范围横幅(§2 头条):scope 接口 + 分页 total 拼当前范围文案,不硬编码账号名 ──
const scope = ref<CustomerScope | null>(null)
const total = ref(0)

function scopeLabel(s: CustomerScope): string {
  let base = ''
  switch (s.kind) {
    case CustomerScopeKind.All: base = t('crm.scope.all'); break
    case CustomerScopeKind.OrgAndChildren: base = t('crm.scope.orgAndChildren', { org: s.orgName }); break
    case CustomerScopeKind.Org: base = t('crm.scope.org', { org: s.orgName }); break
    case CustomerScopeKind.Specified: base = t('crm.scope.specified', { count: s.visibleOrgCount }); break
    case CustomerScopeKind.None: base = s.includeSelf ? '' : t('crm.scope.none'); break
  }
  if (s.includeSelf) return base ? t('crm.scope.withSelf', { scope: base }) : t('crm.scope.selfOnly')
  return base
}

const scopeText = ref('')
function refreshScopeText() {
  scopeText.value = scope.value ? t('crm.scope.label', { scope: scopeLabel(scope.value), total: total.value }) : ''
}

onMounted(async () => {
  try {
    scope.value = await customerApi.scope()
    refreshScopeText()
  } catch (e) {
    message.error(translateError(e))
  }
})

const statusOptions = [
  { label: () => t('crm.statusOptions.new'), value: CustomerStatus.New },
  { label: () => t('crm.statusOptions.following'), value: CustomerStatus.Following },
  { label: () => t('crm.statusOptions.won'), value: CustomerStatus.Won },
  { label: () => t('crm.statusOptions.lost'), value: CustomerStatus.Lost },
]
const statusLabel = (s: CustomerStatus) => statusOptions.find((o) => o.value === s)?.label() ?? ''
const statusTagType = (s: CustomerStatus): 'default' | 'info' | 'success' | 'error' => {
  switch (s) {
    case CustomerStatus.Following: return 'info'
    case CustomerStatus.Won: return 'success'
    case CustomerStatus.Lost: return 'error'
    default: return 'default'
  }
}

// 行数据 → 入参(openEdit 回填用)
const toInput = (r: Customer): CustomerInput => ({
  name: r.name, contact: r.contact, phone: r.phone, intendedAmount: r.intendedAmount, status: r.status,
})

const columns: ProTableColumn<Customer>[] = [
  { key: 'name', title: () => t('crm.name'), search: true },
  { key: 'contact', title: () => t('crm.contact') },
  { key: 'phone', title: () => t('crm.phone') },
  { key: 'intendedAmount', title: () => t('crm.intendedAmount'), width: 120 },
  {
    key: 'status',
    title: () => t('crm.status'),
    width: 100,
    render: (r) => h(NTag, { type: statusTagType(r.status), size: 'small' }, () => statusLabel(r.status)),
  },
  { key: 'createTime', title: () => t('common.createTime'), format: 'datetime' },
  {
    key: 'op',
    title: () => t('common.operation'),
    width: 140,
    hideInSetting: true,
    // 编辑/删除按钮码与后端 [RolePermission] 一致;演示三账号只授了只读 + 导入导出。
    render: (r) => {
      const canUpdate = auth.hasPerm('PUT:/api/v1/biz/customer/{id}')
      const canDelete = auth.hasPerm('DELETE:/api/v1/biz/customer/{id}')
      if (!canUpdate && !canDelete) return null
      return h(NSpace, { size: 4, wrapItem: false }, () => [
        canUpdate && h(NButton, {
          size: 'small', quaternary: true, type: 'primary',
          onClick: () => openEdit(r),
        }, () => t('common.edit')),
        canDelete && h(NPopconfirm, {
          onPositiveClick: () =>
            run(() => customerApi.remove(r.id), t('crm.deleted'))
              .then((ok) => { if (ok) tableRef.value?.refresh() }),
        }, {
          trigger: () => h(NButton, {
            size: 'small', quaternary: true, type: 'error',
          }, () => t('common.delete')),
          default: () => t('crm.deleteConfirm', { name: r.name }),
        }),
      ])
    },
  },
]

function onLoaded(_rows: Customer[], loadedTotal: number) {
  total.value = loadedTotal
  refreshScopeText()
}

// ── 导入 / 导出(公开 demo 下 commit dry-run,见 CrmDemo:ImportDryRun) ──
const importShow = ref(false)
const exportShow = ref(false)
const exporting = ref(false)

const customerExportColumns: ExportColumnDef[] = [
  { key: 'Name', title: '客户名称' },
  { key: 'Contact', title: '联系人' },
  { key: 'Phone', title: '联系电话' },
  { key: 'IntendedAmount', title: '意向金额' },
  { key: 'Status', title: '跟进状态' },
  { key: 'CreateTime', title: '创建时间', defaultSelected: false },
]

const customerImportApi: ImportWizardApi = {
  downloadTemplate: () => customerApi.importTemplate(),
  preview: (file, mapping) => customerApi.importPreview(file, mapping),
  validate: (rows) => customerApi.importValidate(rows),
  errorReport: (rows) => customerApi.importErrorReport(rows),
  commit: async (rows, strategy) => {
    const result = await customerApi.importCommit(rows, strategy)
    // dry-run:计数是「若提交会怎样」,明确提示未写库,避免误解成真落库。
    if (result.dryRun) {
      message.info(t('crm.importDryRunHint', {
        inserted: result.inserted,
        updated: result.updated,
        skipped: result.skipped,
      }))
    }
    // ImportWizard 期望 ImportCommitResult;dryRun 字段多出来无害。
    return result
  },
}

async function onExport(keys: string[]) {
  const p = tableRef.value?.params ?? {}
  exporting.value = true
  try {
    const blob = await customerApi.export({
      name: (p.name as string) || undefined,
      sortField: (p.sortField as string) || undefined,
      sortOrder: (p.sortOrder as string) || undefined,
      columns: keys.join(','),
    })
    triggerBlobDownload(blob, '客户导出.xlsx')
    exportShow.value = false
    message.success(t('export.done'))
  } catch (e) {
    message.error(translateError(e))
  } finally {
    exporting.value = false
  }
}

// ── 新增/编辑弹窗 ──
const show = ref(false)
const formRef = ref<FormInst | null>(null)
const editingId = ref<number | null>(null)
const rules: FormRules = {
  name: { required: true, whitespace: true, message: () => t('crm.nameRequired'), trigger: ['input', 'blur'] },
  contact: { required: true, whitespace: true, message: () => t('crm.contactRequired'), trigger: ['input', 'blur'] },
}
const blank = (): CustomerInput => ({ name: '', contact: '', phone: null, intendedAmount: 0, status: CustomerStatus.New })
const form = reactive<CustomerInput>(blank())

function openAdd() {
  editingId.value = null
  Object.assign(form, blank())
  show.value = true
}
function openEdit(r: Customer) {
  editingId.value = r.id
  Object.assign(form, toInput(r))
  show.value = true
}
async function save() {
  await formRef.value?.validate()
  try {
    if (editingId.value === null) await customerApi.add({ ...form })
    else await customerApi.update(editingId.value, { ...form })
    message.success(t('crm.saved'))
    await tableRef.value?.refresh()
  } catch (e) {
    message.error(translateError(e))
    return false
  }
}
</script>

<template>
  <n-alert v-if="scopeText" type="info" :show-icon="false" style="margin-bottom: 12px">
    {{ scopeText }}
  </n-alert>

  <n-alert type="warning" :show-icon="true" style="margin-bottom: 12px">
    {{ t('crm.importDryRunBanner') }}
  </n-alert>

  <ProTable
    ref="tableRef"
    :columns="columns"
    :fetcher="customerApi.page"
    storage-key="crm-customer"
    @loaded="onLoaded"
    @error="(e) => message.error(translateError(e))"
  >
    <template #toolbar>
      <n-button v-auth="'POST:/api/v1/biz/customer/add'" type="primary" @click="openAdd">
        <template #icon><AppIcon icon="ph:plus" :size="16" /></template>
        {{ t('common.add') }}
      </n-button>
      <n-button v-auth="'POST:/api/v1/biz/customer/import/preview'" @click="importShow = true">
        <template #icon><AppIcon icon="ph:upload-simple" :size="16" /></template>
        {{ t('import.button') }}
      </n-button>
      <n-button v-auth="'GET:/api/v1/biz/customer/export'" @click="exportShow = true">
        <template #icon><AppIcon icon="ph:download-simple" :size="16" /></template>
        {{ t('export.button') }}
      </n-button>
    </template>
  </ProTable>

  <FormContainer
    v-model:show="show"
    :title="editingId === null ? t('crm.addTitle') : t('crm.editTitle')"
    :width="480"
    :on-confirm="save"
    :confirm-text="t('common.save')"
  >
    <n-form ref="formRef" :model="form" :rules="rules" label-placement="left" :label-width="90">
      <n-form-item :label="t('crm.name')" path="name">
        <n-input v-model:value="form.name" :placeholder="t('crm.name')" />
      </n-form-item>
      <n-form-item :label="t('crm.contact')" path="contact">
        <n-input v-model:value="form.contact" :placeholder="t('crm.contact')" />
      </n-form-item>
      <n-form-item :label="t('crm.phone')">
        <n-input v-model:value="form.phone" :placeholder="t('crm.phone')" />
      </n-form-item>
      <n-form-item :label="t('crm.intendedAmount')">
        <n-input-number v-model:value="form.intendedAmount" :min="0" style="width: 100%" />
      </n-form-item>
      <n-form-item :label="t('crm.status')">
        <n-select v-model:value="form.status" :options="statusOptions" />
      </n-form-item>
    </n-form>
  </FormContainer>

  <ImportWizard
    v-model:show="importShow"
    :api="customerImportApi"
    template-file-name="客户导入模板.xlsx"
    error-report-file-name="客户导入错误报告.xlsx"
  />

  <ExportColumnsModal
    v-model:show="exportShow"
    :columns="customerExportColumns"
    :loading="exporting"
    @confirm="onExport"
  />
</template>
