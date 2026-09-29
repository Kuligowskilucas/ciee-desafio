import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { expect, test } from 'vitest'
import App from './App'

test('rota desconhecida mostra página não encontrada e mantém a navegação', () => {
  render(
    <MemoryRouter initialEntries={['/nao-existe']}>
      <App />
    </MemoryRouter>,
  )

  expect(screen.getByRole('heading', { name: 'Página não encontrada' })).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'Candidatos' })).toHaveAttribute('href', '/candidatos')
  expect(screen.getByRole('link', { name: 'Novo candidato' })).toHaveAttribute('href', '/candidatos/novo')
})
