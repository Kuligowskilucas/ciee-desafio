import type { NovoCandidato } from './api'

export const FORMATO_EMAIL = /^[^\s@]+@([^\s@.]+\.)+[^\s@.]{2,}$/

export const FORMATO_TELEFONE =
  /^(?:\+?55[\s.-]?)?(?:\([1-9]{2}\)|[1-9]{2})[\s.-]?(?:9[\s.]?\d{4}|[2-5]\d{3})[\s.-]?\d{4}$/

export const TAMANHO_MAXIMO_PDF_EM_MB = 5
export const TAMANHO_MAXIMO_PDF_EM_BYTES = TAMANHO_MAXIMO_PDF_EM_MB * 1024 * 1024

const ASSINATURA_PDF = '%PDF-'

export type CampoDoCandidato = keyof NovoCandidato

export type ErrosDoCandidato = Partial<Record<CampoDoCandidato, string>>

const CAMPOS_DO_CANDIDATO: CampoDoCandidato[] = [
  'nomeCompleto',
  'email',
  'telefone',
  'areaInteresse',
  'resumoProfissional',
]

export function validarCandidato(candidato: NovoCandidato): ErrosDoCandidato {
  const erros: ErrosDoCandidato = {}
  const nomeCompleto = candidato.nomeCompleto.trim()
  const email = candidato.email.trim()
  const telefone = candidato.telefone.trim()

  if (!nomeCompleto) {
    erros.nomeCompleto = 'Informe o nome completo.'
  }

  if (!email) {
    erros.email = 'Informe o e-mail.'
  } else if (!FORMATO_EMAIL.test(email)) {
    erros.email = 'Informe um e-mail válido, como nome@exemplo.com.'
  }

  if (telefone && !FORMATO_TELEFONE.test(telefone)) {
    erros.telefone = 'Informe um telefone com DDD, como (41) 99999-8888.'
  }

  return erros
}

export async function validarArquivoPdf(arquivo: File): Promise<string | null> {
  if (arquivo.size > TAMANHO_MAXIMO_PDF_EM_BYTES) {
    return `O arquivo tem mais de ${TAMANHO_MAXIMO_PDF_EM_MB} MB. Envie um PDF de até ${TAMANHO_MAXIMO_PDF_EM_MB} MB.`
  }

  const inicio = await arquivo.slice(0, ASSINATURA_PDF.length).text()
  if (inicio !== ASSINATURA_PDF) {
    return 'O arquivo enviado não é um PDF. Selecione um arquivo .pdf.'
  }

  return null
}

export function errosDoProblema(errors: Record<string, string[]>) {
  const campos: ErrosDoCandidato = {}
  const gerais: string[] = []

  for (const [chave, mensagens] of Object.entries(errors)) {
    const campo = CAMPOS_DO_CANDIDATO.find((nome) => nome.toLowerCase() === chave.toLowerCase())
    if (campo) {
      campos[campo] = mensagens.join(' ')
    } else {
      gerais.push(...mensagens)
    }
  }

  return { campos, gerais }
}
