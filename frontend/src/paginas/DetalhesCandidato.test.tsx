import { render, screen } from '@testing-library/react'
import { MemoryRouter, useLocation } from 'react-router'
import { beforeEach, expect, test, vi } from 'vitest'
import App from '../App'
import type { CandidatoDto } from '../api'

const candidato: CandidatoDto = {
  id: 42,
  nomeCompleto: 'Maria da Silva',
  email: 'maria@exemplo.com',
  telefone: '(41) 99999-8888',
  areaInteresse: null,
  resumoProfissional: null,
  criadoEm: '2026-09-29T18:00:00+00:00',
}

interface EntradaDoHistorico {
  pathname: string
  state: unknown
}

beforeEach(() => {
  vi.stubGlobal(
    'fetch',
    vi.fn<typeof fetch>(
      async () =>
        new Response(JSON.stringify(candidato), { status: 200, headers: { 'Content-Type': 'application/json' } }),
    ),
  )
})

function EntradaAtual() {
  const { pathname, state } = useLocation()
  return <pre data-testid="entrada-atual">{JSON.stringify({ pathname, state })}</pre>
}

function abrir(entrada: EntradaDoHistorico) {
  return render(
    <MemoryRouter initialEntries={[entrada]}>
      <App />
      <EntradaAtual />
    </MemoryRouter>,
  )
}

function entradaAtual(): EntradaDoHistorico {
  return JSON.parse(screen.getByTestId('entrada-atual').textContent ?? '')
}

test('mensagem de sucesso aparece após o cadastro e não volta ao recarregar a página', async () => {
  const tela = abrir({ pathname: '/candidatos/42', state: { mensagem: 'Cadastro salvo com sucesso.' } })

  expect(await screen.findByRole('heading', { name: 'Maria da Silva' })).toBeInTheDocument()
  expect(screen.getByText('Cadastro salvo com sucesso.')).toBeInTheDocument()
  expect(entradaAtual()).toEqual({ pathname: '/candidatos/42', state: null })

  const entradaNoMomentoDoRecarregamento = entradaAtual()
  tela.unmount()
  abrir(entradaNoMomentoDoRecarregamento)

  expect(await screen.findByRole('heading', { name: 'Maria da Silva' })).toBeInTheDocument()
  expect(screen.queryByText('Cadastro salvo com sucesso.')).not.toBeInTheDocument()
})
