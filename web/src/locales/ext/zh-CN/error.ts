// 键要和后端未标注 MsgKey 的兜底键逐字对上:error.code.{数值}(BizErrorCode.CustomerNotFound = 60001)。
export default {
  code: {
    60001: '客户不存在或不在当前数据范围内',
  },
}
