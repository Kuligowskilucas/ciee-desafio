const formatoDataHora = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' })

export function formatarDataHora(dataIso: string) {
  return formatoDataHora.format(new Date(dataIso))
}
