export interface CandidatoDto {
  id: number
  nomeCompleto: string
  email: string
  telefone: string | null
  areaInteresse: string | null
  resumoProfissional: string | null
  criadoEm: string
}

export interface CandidatoResumoDto {
  id: number
  nomeCompleto: string
  email: string
  areaInteresse: string | null
  criadoEm: string
}

export interface DadosCurriculoDto {
  nomeCompleto: string | null
  email: string | null
  telefone: string | null
}

export interface NovoCandidato {
  nomeCompleto: string
  email: string
  telefone: string
  areaInteresse: string
  resumoProfissional: string
}

export interface ProblemDetails {
  status: number
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export class ErroDaApi extends Error {
  readonly problema: ProblemDetails

  constructor(problema: ProblemDetails) {
    super(problema.detail ?? problema.title ?? `A API respondeu com o status ${problema.status}.`)
    this.name = 'ErroDaApi'
    this.problema = problema
  }
}

export class ApiIndisponivel extends Error {
  constructor() {
    super('Não foi possível conectar à API. Verifique se o backend está rodando e tente novamente.')
    this.name = 'ApiIndisponivel'
  }
}

export function mensagemDoErro(erro: unknown, mensagemPadrao: string): string {
  if (erro instanceof ApiIndisponivel) {
    return erro.message
  }

  if (erro instanceof ErroDaApi) {
    if (erro.problema.detail) {
      return erro.problema.detail
    }
    if (erro.problema.status >= 500) {
      return 'Ocorreu um erro no servidor. Tente novamente.'
    }
  }

  return mensagemPadrao
}

export function listarCandidatos(signal?: AbortSignal) {
  return requisitar<CandidatoResumoDto[]>('/api/candidatos', { signal })
}

export function obterCandidato(id: string, signal?: AbortSignal) {
  return requisitar<CandidatoDto>(`/api/candidatos/${encodeURIComponent(id)}`, { signal })
}

export function cadastrarCandidato(candidato: NovoCandidato) {
  return requisitar<CandidatoDto>('/api/candidatos', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(candidato),
  })
}

export function extrairCurriculo(arquivo: File) {
  const formulario = new FormData()
  formulario.append('arquivo', arquivo)
  return requisitar<DadosCurriculoDto>('/api/curriculos/extrair', { method: 'POST', body: formulario })
}

async function requisitar<T>(url: string, opcoes: RequestInit): Promise<T> {
  let resposta: Response
  try {
    resposta = await fetch(url, opcoes)
  } catch (erro) {
    if (opcoes.signal?.aborted) {
      throw erro
    }
    throw new ApiIndisponivel()
  }

  if (resposta.ok) {
    return (await resposta.json()) as T
  }

  if (resposta.headers.get('Content-Type')?.includes('application/problem+json')) {
    throw new ErroDaApi((await resposta.json()) as ProblemDetails)
  }

  if (resposta.status >= 500) {
    throw new ApiIndisponivel()
  }

  throw new ErroDaApi({ status: resposta.status })
}
