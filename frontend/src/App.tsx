import { NavLink, Navigate, Route, Routes } from 'react-router'
import { CadastroCandidato } from './paginas/CadastroCandidato'
import { DetalhesCandidato } from './paginas/DetalhesCandidato'
import { ListaCandidatos } from './paginas/ListaCandidatos'

export default function App() {
  return (
    <>
      <header className="cabecalho">
        <span className="marca">Cadastro de candidatos</span>
        <nav>
          <NavLink to="/candidatos" end>
            Candidatos
          </NavLink>
          <NavLink to="/candidatos/novo">Novo candidato</NavLink>
        </nav>
      </header>
      <main className="conteudo">
        <Routes>
          <Route path="/" element={<Navigate to="/candidatos" replace />} />
          <Route path="/candidatos" element={<ListaCandidatos />} />
          <Route path="/candidatos/novo" element={<CadastroCandidato />} />
          <Route path="/candidatos/:id" element={<DetalhesCandidato />} />
          <Route path="*" element={<PaginaNaoEncontrada />} />
        </Routes>
      </main>
    </>
  )
}

function PaginaNaoEncontrada() {
  return (
    <section>
      <h1>Página não encontrada</h1>
      <p>O endereço acessado não existe.</p>
    </section>
  )
}
