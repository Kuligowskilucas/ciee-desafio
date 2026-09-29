import { useEffect, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router'
import { ErroDaApi, mensagemDoErro, obterCandidato, type CandidatoDto } from '../api'
import { formatarDataHora } from '../formatacao'

type Estado =
  | { tipo: 'carregando' }
  | { tipo: 'carregado'; candidato: CandidatoDto }
  | { tipo: 'naoEncontrado' }
  | { tipo: 'falhou'; mensagem: string }

interface Resultado {
  id: string
  estado: Estado
}

interface EstadoDaNavegacao {
  mensagem?: string
}

export function DetalhesCandidato() {
  const { id = '' } = useParams()
  const location = useLocation()
  const navigate = useNavigate()
  const [mensagemDeSucesso] = useState(() => (location.state as EstadoDaNavegacao | null)?.mensagem)
  const [resultado, setResultado] = useState<Resultado | null>(null)
  const estado: Estado = resultado?.id === id ? resultado.estado : { tipo: 'carregando' }

  useEffect(() => {
    if (location.state) {
      navigate(location.pathname, { replace: true, state: null })
    }
  }, [location.state, location.pathname, navigate])

  useEffect(() => {
    const controle = new AbortController()
    obterCandidato(id, controle.signal)
      .then((candidato) => setResultado({ id, estado: { tipo: 'carregado', candidato } }))
      .catch((erro: unknown) => {
        if (controle.signal.aborted) {
          return
        }
        if (erro instanceof ErroDaApi && erro.problema.status === 404) {
          setResultado({ id, estado: { tipo: 'naoEncontrado' } })
        } else {
          const mensagem = mensagemDoErro(erro, 'Não foi possível carregar o candidato.')
          setResultado({ id, estado: { tipo: 'falhou', mensagem } })
        }
      })
    return () => controle.abort()
  }, [id])

  return (
    <section>
      {mensagemDeSucesso && (
        <p role="status" className="mensagem sucesso">
          {mensagemDeSucesso}
        </p>
      )}
      {estado.tipo === 'carregando' && <p>Carregando candidato…</p>}
      {estado.tipo === 'naoEncontrado' && <h1>Candidato não encontrado</h1>}
      {estado.tipo === 'falhou' && (
        <p role="alert" className="mensagem erro">
          {estado.mensagem}
        </p>
      )}
      {estado.tipo === 'carregado' && <Candidato candidato={estado.candidato} />}
      <p>
        <Link to="/candidatos">Voltar para a lista</Link>
      </p>
    </section>
  )
}

function Candidato({ candidato }: { candidato: CandidatoDto }) {
  return (
    <article className="detalhes">
      <h1>{candidato.nomeCompleto}</h1>
      <dl>
        <dt>E-mail</dt>
        <dd>{candidato.email}</dd>
        <dt>Telefone</dt>
        <dd>{candidato.telefone ?? 'Não informado'}</dd>
        <dt>Área ou cargo de interesse</dt>
        <dd>{candidato.areaInteresse ?? 'Não informada'}</dd>
        <dt>Resumo profissional</dt>
        <dd className="resumo">{candidato.resumoProfissional ?? 'Não informado'}</dd>
        <dt>Cadastrado em</dt>
        <dd>{formatarDataHora(candidato.criadoEm)}</dd>
      </dl>
    </article>
  )
}
