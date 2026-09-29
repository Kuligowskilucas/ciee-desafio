import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { ErroDaApi, cadastrarCandidato, mensagemDoErro, type DadosCurriculoDto, type NovoCandidato } from '../api'
import { Campo } from '../componentes/Campo'
import { ImportacaoPdf } from '../componentes/ImportacaoPdf'
import { errosDoProblema, validarCandidato, type CampoDoCandidato, type ErrosDoCandidato } from '../validacao'

const candidatoVazio: NovoCandidato = {
  nomeCompleto: '',
  email: '',
  telefone: '',
  areaInteresse: '',
  resumoProfissional: '',
}

export function CadastroCandidato() {
  const navigate = useNavigate()
  const [candidato, setCandidato] = useState(candidatoVazio)
  const [erros, setErros] = useState<ErrosDoCandidato>({})
  const [errosGerais, setErrosGerais] = useState<string[]>([])
  const [enviando, setEnviando] = useState(false)

  function alterar(campo: CampoDoCandidato, valor: string) {
    setCandidato((atual) => ({ ...atual, [campo]: valor }))
    setErros((atuais) => ({ ...atuais, [campo]: undefined }))
  }

  function preencherComCurriculo(dados: DadosCurriculoDto) {
    const encontrados = Object.entries(dados).filter(([, valor]) => valor)
    setCandidato((atual) => ({ ...atual, ...Object.fromEntries(encontrados) }))
    setErros((atuais) => ({ ...atuais, ...Object.fromEntries(encontrados.map(([campo]) => [campo, undefined])) }))
  }

  async function enviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault()
    setErrosGerais([])

    const errosEncontrados = validarCandidato(candidato)
    if (Object.keys(errosEncontrados).length > 0) {
      setErros(errosEncontrados)
      return
    }

    setEnviando(true)
    try {
      const criado = await cadastrarCandidato(candidato)
      navigate(`/candidatos/${criado.id}`, { state: { mensagem: 'Cadastro salvo com sucesso.' } })
    } catch (erro) {
      mostrarErroDoEnvio(erro)
    } finally {
      setEnviando(false)
    }
  }

  function mostrarErroDoEnvio(erro: unknown) {
    if (erro instanceof ErroDaApi && erro.problema.status === 400 && erro.problema.errors) {
      const { campos, gerais } = errosDoProblema(erro.problema.errors)
      setErros(campos)
      setErrosGerais(gerais)
    } else if (erro instanceof ErroDaApi && erro.problema.status === 409) {
      setErros({ email: mensagemDoErro(erro, 'Já existe um candidato cadastrado com este e-mail.') })
    } else {
      setErrosGerais([mensagemDoErro(erro, 'Não foi possível salvar o cadastro. Tente novamente.')])
    }
  }

  return (
    <section>
      <h1>Novo candidato</h1>
      <ImportacaoPdf aoExtrair={preencherComCurriculo} />

      <form className="formulario" noValidate onSubmit={enviar}>
        <h2>Dados do candidato</h2>
        {errosGerais.length > 0 && (
          <div role="alert" className="mensagem erro">
            {errosGerais.map((mensagem) => (
              <p key={mensagem}>{mensagem}</p>
            ))}
          </div>
        )}
        <Campo
          id="nomeCompleto"
          rotulo="Nome completo"
          obrigatorio
          valor={candidato.nomeCompleto}
          erro={erros.nomeCompleto}
          aoAlterar={(valor) => alterar('nomeCompleto', valor)}
        />
        <Campo
          id="email"
          rotulo="E-mail"
          tipo="email"
          obrigatorio
          valor={candidato.email}
          erro={erros.email}
          aoAlterar={(valor) => alterar('email', valor)}
        />
        <Campo
          id="telefone"
          rotulo="Telefone"
          tipo="tel"
          valor={candidato.telefone}
          erro={erros.telefone}
          aoAlterar={(valor) => alterar('telefone', valor)}
        />
        <Campo
          id="areaInteresse"
          rotulo="Área ou cargo de interesse"
          valor={candidato.areaInteresse}
          erro={erros.areaInteresse}
          aoAlterar={(valor) => alterar('areaInteresse', valor)}
        />
        <Campo
          id="resumoProfissional"
          rotulo="Resumo profissional"
          multilinha
          valor={candidato.resumoProfissional}
          erro={erros.resumoProfissional}
          aoAlterar={(valor) => alterar('resumoProfissional', valor)}
        />
        <button type="submit" disabled={enviando}>
          {enviando ? 'Salvando…' : 'Salvar cadastro'}
        </button>
      </form>
    </section>
  )
}
