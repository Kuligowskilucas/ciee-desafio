import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { listarCandidatos, mensagemDoErro, type CandidatoResumoDto } from '../api'
import { formatarDataHora } from '../formatacao'

type Estado =
  | { tipo: 'carregando' }
  | { tipo: 'carregada'; candidatos: CandidatoResumoDto[] }
  | { tipo: 'falhou'; mensagem: string }

export function ListaCandidatos() {
  const [estado, setEstado] = useState<Estado>({ tipo: 'carregando' })

  useEffect(() => {
    const controle = new AbortController()
    listarCandidatos(controle.signal)
      .then((candidatos) => setEstado({ tipo: 'carregada', candidatos }))
      .catch((erro: unknown) => {
        if (!controle.signal.aborted) {
          setEstado({ tipo: 'falhou', mensagem: mensagemDoErro(erro, 'Não foi possível carregar os candidatos.') })
        }
      })
    return () => controle.abort()
  }, [])

  return (
    <section>
      <div className="titulo-com-acao">
        <h1>Candidatos</h1>
        <Link className="botao" to="/candidatos/novo">
          Novo candidato
        </Link>
      </div>

      {estado.tipo === 'carregando' && <p role="status">Carregando candidatos…</p>}
      {estado.tipo === 'falhou' && (
        <p role="alert" className="mensagem erro">
          {estado.mensagem}
        </p>
      )}
      {estado.tipo === 'carregada' && estado.candidatos.length === 0 && <p>Nenhum candidato cadastrado ainda.</p>}
      {estado.tipo === 'carregada' && estado.candidatos.length > 0 && (
        <table className="tabela">
          <thead>
            <tr>
              <th scope="col">Nome</th>
              <th scope="col">E-mail</th>
              <th scope="col">Área de interesse</th>
              <th scope="col">Cadastrado em</th>
            </tr>
          </thead>
          <tbody>
            {estado.candidatos.map((candidato) => (
              <tr key={candidato.id}>
                <td>
                  <Link to={`/candidatos/${candidato.id}`}>{candidato.nomeCompleto}</Link>
                </td>
                <td>{candidato.email}</td>
                <td>{candidato.areaInteresse ?? '—'}</td>
                <td>{formatarDataHora(candidato.criadoEm)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  )
}
