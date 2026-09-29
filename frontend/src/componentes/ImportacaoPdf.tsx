import { useState, type ChangeEvent } from 'react'
import { extrairCurriculo, mensagemDoErro, type DadosCurriculoDto } from '../api'
import { TAMANHO_MAXIMO_PDF_EM_MB, validarArquivoPdf } from '../validacao'

type Situacao =
  | { tipo: 'aguardando' }
  | { tipo: 'lendo'; nomeDoArquivo: string }
  | { tipo: 'concluida'; mensagem: string }
  | { tipo: 'falhou'; mensagem: string }

const CAMPOS_EXTRAIDOS = [
  { campo: 'nomeCompleto', nome: 'nome' },
  { campo: 'email', nome: 'e-mail' },
  { campo: 'telefone', nome: 'telefone' },
] as const

const listaEmPortugues = new Intl.ListFormat('pt-BR', { type: 'conjunction' })

interface ImportacaoPdfProps {
  aoExtrair: (dados: DadosCurriculoDto) => void
}

export function ImportacaoPdf({ aoExtrair }: ImportacaoPdfProps) {
  const [situacao, setSituacao] = useState<Situacao>({ tipo: 'aguardando' })

  async function aoEscolherArquivo(evento: ChangeEvent<HTMLInputElement>) {
    const entrada = evento.currentTarget
    const arquivo = entrada.files?.[0]
    if (!arquivo) {
      return
    }

    const problemaNoArquivo = await validarArquivoPdf(arquivo)
    if (problemaNoArquivo) {
      entrada.value = ''
      setSituacao({ tipo: 'falhou', mensagem: problemaNoArquivo })
      return
    }

    setSituacao({ tipo: 'lendo', nomeDoArquivo: arquivo.name })
    try {
      const dados = await extrairCurriculo(arquivo)
      aoExtrair(dados)
      setSituacao({ tipo: 'concluida', mensagem: resumoDaExtracao(dados) })
    } catch (erro) {
      entrada.value = ''
      setSituacao({
        tipo: 'falhou',
        mensagem: mensagemDoErro(erro, 'Não foi possível ler o PDF. Preencha os dados manualmente.'),
      })
    }
  }

  return (
    <section className="importacao" aria-labelledby="titulo-importacao">
      <h2 id="titulo-importacao">Importar currículo em PDF (opcional)</h2>
      <p className="dica">
        Os dados encontrados preenchem o formulário abaixo, e você pode corrigi-los antes de salvar.
      </p>
      <label htmlFor="arquivo">Arquivo PDF (até {TAMANHO_MAXIMO_PDF_EM_MB} MB)</label>
      <input
        id="arquivo"
        type="file"
        accept=".pdf,application/pdf"
        disabled={situacao.tipo === 'lendo'}
        onChange={aoEscolherArquivo}
      />
      {situacao.tipo === 'lendo' && <p role="status">Lendo "{situacao.nomeDoArquivo}"…</p>}
      {situacao.tipo === 'concluida' && (
        <p role="status" className="mensagem sucesso">
          {situacao.mensagem}
        </p>
      )}
      {situacao.tipo === 'falhou' && (
        <p role="alert" className="mensagem erro">
          {situacao.mensagem}
        </p>
      )}
    </section>
  )
}

function resumoDaExtracao(dados: DadosCurriculoDto) {
  const encontrados = CAMPOS_EXTRAIDOS.filter(({ campo }) => dados[campo]).map(({ nome }) => nome)
  const naoEncontrados = CAMPOS_EXTRAIDOS.filter(({ campo }) => !dados[campo]).map(({ nome }) => nome)

  if (encontrados.length === 0) {
    return 'Não identificamos nome, e-mail nem telefone neste PDF. Preencha os dados manualmente.'
  }

  const partes = [`Preenchemos ${listaEmPortugues.format(encontrados)} a partir do PDF.`]
  if (naoEncontrados.length > 0) {
    partes.push(`Não encontramos ${listaEmPortugues.format(naoEncontrados)}; preencha manualmente.`)
  }
  partes.push('Confira os dados antes de salvar.')
  return partes.join(' ')
}
