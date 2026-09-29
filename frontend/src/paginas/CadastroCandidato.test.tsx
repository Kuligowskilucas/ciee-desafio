import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useLocation, useParams } from 'react-router'
import { beforeEach, describe, expect, test, vi } from 'vitest'
import { TAMANHO_MAXIMO_PDF_EM_BYTES } from '../validacao'
import { CadastroCandidato } from './CadastroCandidato'

const MENSAGEM_API_FORA_DO_AR =
  'Não foi possível conectar à API. Verifique se o backend está rodando e tente novamente.'

const fetchMock = vi.fn<typeof fetch>()

beforeEach(() => {
  fetchMock.mockReset()
  vi.stubGlobal('fetch', fetchMock)
})

function json(status: number, corpo: unknown) {
  return new Response(JSON.stringify(corpo), { status, headers: { 'Content-Type': 'application/json' } })
}

function problema(status: number, corpo: Record<string, unknown>) {
  return new Response(JSON.stringify({ status, ...corpo }), {
    status,
    headers: { 'Content-Type': 'application/problem+json' },
  })
}

function pdf(nome = 'curriculo.pdf', conteudo: BlobPart[] = ['%PDF-1.7 conteudo']) {
  return new File(conteudo, nome, { type: 'application/pdf' })
}

function DetalhesDeTeste() {
  const { id } = useParams()
  const { state } = useLocation()
  return (
    <p>
      Detalhes do candidato {id}: {state?.mensagem}
    </p>
  )
}

function renderizar() {
  render(
    <MemoryRouter initialEntries={['/candidatos/novo']}>
      <Routes>
        <Route path="/candidatos/novo" element={<CadastroCandidato />} />
        <Route path="/candidatos/:id" element={<DetalhesDeTeste />} />
      </Routes>
    </MemoryRouter>,
  )

  return {
    usuario: userEvent.setup(),
    nome: () => screen.getByRole('textbox', { name: 'Nome completo' }),
    email: () => screen.getByRole('textbox', { name: 'E-mail' }),
    telefone: () => screen.getByRole('textbox', { name: 'Telefone' }),
    arquivo: () => screen.getByLabelText('Arquivo PDF (até 5 MB)'),
    salvar: () => screen.getByRole('button', { name: 'Salvar cadastro' }),
  }
}

async function preencherCandidatoValido(tela: ReturnType<typeof renderizar>) {
  await tela.usuario.type(tela.nome(), 'Maria da Silva')
  await tela.usuario.type(tela.email(), 'maria@exemplo.com')
}

describe('validação no front', () => {
  test('mostra os erros nos campos e não chama a API', async () => {
    const tela = renderizar()
    await tela.usuario.type(tela.email(), 'maria@exemplo')

    await tela.usuario.click(tela.salvar())

    expect(tela.nome()).toBeInvalid()
    expect(tela.nome()).toHaveAccessibleDescription('Informe o nome completo.')
    expect(tela.email()).toHaveAccessibleDescription('Informe um e-mail válido, como nome@exemplo.com.')
    expect(fetchMock).not.toHaveBeenCalled()
  })

  test('editar o campo apaga o erro dele', async () => {
    const tela = renderizar()
    await tela.usuario.click(tela.salvar())

    await tela.usuario.type(tela.nome(), 'M')

    expect(tela.nome()).not.toHaveAttribute('aria-invalid')
    expect(screen.queryByText('Informe o nome completo.')).not.toBeInTheDocument()
    expect(screen.getByText('Informe o e-mail.')).toBeInTheDocument()
  })
})

describe('importação do PDF', () => {
  test('preenche o que foi encontrado, mantém o resto e os campos continuam editáveis', async () => {
    const tela = renderizar()
    await tela.usuario.type(tela.nome(), 'Nome Digitado Antes')
    await tela.usuario.type(tela.telefone(), '(41) 3333-4444')
    fetchMock.mockResolvedValueOnce(
      json(200, { nomeCompleto: 'Mariana Alves Ferreira', email: 'mariana@example.com', telefone: null }),
    )
    const arquivo = pdf()

    await tela.usuario.upload(tela.arquivo(), arquivo)

    expect(await screen.findByRole('status')).toHaveTextContent(
      'Preenchemos nome e e-mail a partir do PDF. Não encontramos telefone; preencha manualmente. ' +
        'Confira os dados antes de salvar.',
    )
    expect(tela.nome()).toHaveValue('Mariana Alves Ferreira')
    expect(tela.email()).toHaveValue('mariana@example.com')
    expect(tela.telefone()).toHaveValue('(41) 3333-4444')

    const [url, opcoes = {}] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/curriculos/extrair')
    expect(opcoes.method).toBe('POST')
    expect((opcoes.body as FormData).get('arquivo')).toBe(arquivo)

    await tela.usuario.type(tela.nome(), ' Souza')
    expect(tela.nome()).toHaveValue('Mariana Alves Ferreira Souza')
  })

  test('avisa quando nada é identificado', async () => {
    const tela = renderizar()
    fetchMock.mockResolvedValueOnce(json(200, { nomeCompleto: null, email: null, telefone: null }))

    await tela.usuario.upload(tela.arquivo(), pdf())

    expect(await screen.findByRole('status')).toHaveTextContent(
      'Não identificamos nome, e-mail nem telefone neste PDF. Preencha os dados manualmente.',
    )
  })

  test.each([
    ['maior que 5 MB', pdf('grande.pdf', [new Uint8Array(TAMANHO_MAXIMO_PDF_EM_BYTES + 1)]),
      'O arquivo tem mais de 5 MB. Envie um PDF de até 5 MB.'],
    ['com extensão .pdf mas sem conteúdo de PDF', pdf('renomeado.pdf', ['texto simples']),
      'O arquivo enviado não é um PDF. Selecione um arquivo .pdf.'],
  ])('recusa arquivo %s sem chamar a API', async (_descricao, arquivo, mensagem) => {
    const tela = renderizar()

    await tela.usuario.upload(tela.arquivo(), arquivo)

    expect(await screen.findByRole('alert')).toHaveTextContent(mensagem)
    expect(fetchMock).not.toHaveBeenCalled()
  })

  test('falha na leitura mostra a mensagem do backend e não impede o cadastro manual', async () => {
    const tela = renderizar()
    fetchMock
      .mockResolvedValueOnce(
        problema(422, {
          title: 'Não foi possível ler o arquivo',
          detail: 'O PDF está protegido por senha. Envie uma versão sem senha ou preencha os dados manualmente.',
        }),
      )
      .mockResolvedValueOnce(json(201, { id: 7 }))

    await tela.usuario.upload(tela.arquivo(), pdf('protegido.pdf'))
    expect(await screen.findByRole('alert')).toHaveTextContent('O PDF está protegido por senha.')

    await preencherCandidatoValido(tela)
    await tela.usuario.click(tela.salvar())

    expect(await screen.findByText('Detalhes do candidato 7: Cadastro salvo com sucesso.')).toBeInTheDocument()
  })
})

describe('envio do cadastro', () => {
  test('salva e vai para os detalhes com a mensagem de sucesso', async () => {
    const tela = renderizar()
    fetchMock.mockResolvedValueOnce(json(201, { id: 42 }))
    await preencherCandidatoValido(tela)
    await tela.usuario.type(tela.telefone(), '41999998888')

    await tela.usuario.click(tela.salvar())

    expect(await screen.findByText('Detalhes do candidato 42: Cadastro salvo com sucesso.')).toBeInTheDocument()
    const [url, opcoes = {}] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/candidatos')
    expect(opcoes.method).toBe('POST')
    expect(JSON.parse(opcoes.body as string)).toEqual({
      nomeCompleto: 'Maria da Silva',
      email: 'maria@exemplo.com',
      telefone: '41999998888',
      areaInteresse: '',
      resumoProfissional: '',
    })
  })

  test('erro 400 do backend aparece no campo certo', async () => {
    const tela = renderizar()
    fetchMock.mockResolvedValueOnce(
      problema(400, {
        title: 'Dados inválidos',
        errors: { NomeCompleto: ['O nome completo deve ter no máximo 150 caracteres.'] },
      }),
    )
    await preencherCandidatoValido(tela)

    await tela.usuario.click(tela.salvar())

    expect(await screen.findByText('O nome completo deve ter no máximo 150 caracteres.')).toBeInTheDocument()
    expect(tela.nome()).toHaveAccessibleDescription('O nome completo deve ter no máximo 150 caracteres.')
  })

  test('e-mail já cadastrado (409) aparece no campo e-mail', async () => {
    const tela = renderizar()
    fetchMock.mockResolvedValueOnce(
      problema(409, {
        title: 'Conflito com um registro existente',
        detail: 'Já existe um candidato cadastrado com este e-mail.',
      }),
    )
    await preencherCandidatoValido(tela)

    await tela.usuario.click(tela.salvar())

    expect(await screen.findByText('Já existe um candidato cadastrado com este e-mail.')).toBeInTheDocument()
    expect(tela.email()).toHaveAccessibleDescription('Já existe um candidato cadastrado com este e-mail.')
  })

  test.each([
    ['o fetch falha (sem conexão)', () => fetchMock.mockRejectedValueOnce(new TypeError('Failed to fetch'))],
    ['o proxy responde 502 em texto', () =>
      fetchMock.mockResolvedValueOnce(new Response('', { status: 502, headers: { 'Content-Type': 'text/plain' } }))],
  ])('mostra "API fora do ar" quando %s', async (_descricao, configurarResposta) => {
    const tela = renderizar()
    configurarResposta()
    await preencherCandidatoValido(tela)

    await tela.usuario.click(tela.salvar())

    expect(await screen.findByRole('alert')).toHaveTextContent(MENSAGEM_API_FORA_DO_AR)
    expect(tela.salvar()).toBeEnabled()
  })
})
