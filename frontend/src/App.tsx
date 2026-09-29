import { NavLink, Route, Routes } from 'react-router'

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
