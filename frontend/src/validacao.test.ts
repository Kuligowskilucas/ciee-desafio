import { describe, expect, test } from 'vitest'
import casos from '../../casos-de-validacao.json'
import type { NovoCandidato } from './api'
import {
  FORMATO_EMAIL,
  FORMATO_TELEFONE,
  TAMANHO_MAXIMO_PDF_EM_BYTES,
  errosDoProblema,
  validarArquivoPdf,
  validarCandidato,
} from './validacao'

const candidatoValido: NovoCandidato = {
  nomeCompleto: 'Maria da Silva',
  email: 'maria@exemplo.com',
  telefone: '',
  areaInteresse: '',
  resumoProfissional: '',
}

function arquivo(conteudo: BlobPart[], nome = 'curriculo.pdf') {
  return new File(conteudo, nome, { type: 'application/pdf' })
}

function pdfComTamanho(tamanhoEmBytes: number) {
  const bytes = new Uint8Array(tamanhoEmBytes)
  bytes.set(new TextEncoder().encode('%PDF-1.7'))
  return arquivo([bytes])
}

describe('regras compartilhadas com o backend (casos-de-validacao.json)', () => {
  test.each(casos.email.validos)('aceita o e-mail %s', (email) => {
    expect(FORMATO_EMAIL.test(email)).toBe(true)
  })

  test.each(casos.email.invalidos)('recusa o e-mail %s', (email) => {
    expect(FORMATO_EMAIL.test(email)).toBe(false)
  })

  test.each(casos.telefone.validos.map((caso) => caso.valor))('aceita o telefone %s', (telefone) => {
    expect(FORMATO_TELEFONE.test(telefone)).toBe(true)
  })

  test.each(casos.telefone.invalidos)('recusa o telefone %s', (telefone) => {
    expect(FORMATO_TELEFONE.test(telefone)).toBe(false)
  })
})

describe('validarCandidato', () => {
  test('dados válidos não geram erros', () => {
    expect(validarCandidato(candidatoValido)).toEqual({})
  })

  test.each(['', '   '])('nome %j é recusado', (nomeCompleto) => {
    expect(validarCandidato({ ...candidatoValido, nomeCompleto })).toEqual({
      nomeCompleto: 'Informe o nome completo.',
    })
  })

  test('e-mail vazio é recusado', () => {
    expect(validarCandidato({ ...candidatoValido, email: '  ' })).toEqual({ email: 'Informe o e-mail.' })
  })

  test('e-mail fora do formato é recusado', () => {
    expect(validarCandidato({ ...candidatoValido, email: 'maria@exemplo' })).toEqual({
      email: 'Informe um e-mail válido, como nome@exemplo.com.',
    })
  })

  test('e-mail com espaços nas pontas é aceito, como no backend', () => {
    expect(validarCandidato({ ...candidatoValido, email: '  maria@exemplo.com  ' })).toEqual({})
  })

  test('telefone é opcional', () => {
    expect(validarCandidato({ ...candidatoValido, telefone: '   ' })).toEqual({})
  })

  test('telefone fora do formato é recusado', () => {
    expect(validarCandidato({ ...candidatoValido, telefone: 'abc' })).toEqual({
      telefone: 'Informe um telefone com DDD, como (41) 99999-8888.',
    })
  })

  test('mostra todos os erros de uma vez', () => {
    const erros = validarCandidato({ ...candidatoValido, nomeCompleto: '', email: 'x', telefone: '123' })

    expect(Object.keys(erros)).toEqual(['nomeCompleto', 'email', 'telefone'])
  })
})

describe('validarArquivoPdf', () => {
  test('aceita PDF com exatamente 5 MB', async () => {
    expect(await validarArquivoPdf(pdfComTamanho(TAMANHO_MAXIMO_PDF_EM_BYTES))).toBeNull()
  })

  test('recusa arquivo 1 byte acima de 5 MB', async () => {
    expect(await validarArquivoPdf(pdfComTamanho(TAMANHO_MAXIMO_PDF_EM_BYTES + 1))).toBe(
      'O arquivo tem mais de 5 MB. Envie um PDF de até 5 MB.',
    )
  })

  test('recusa arquivo .pdf que não começa com %PDF-', async () => {
    expect(await validarArquivoPdf(arquivo(['Maria da Silva, currículo em texto']))).toBe(
      'O arquivo enviado não é um PDF. Selecione um arquivo .pdf.',
    )
  })

  test('aceita PDF sem a extensão .pdf, como no backend', async () => {
    expect(await validarArquivoPdf(arquivo(['%PDF-1.7'], 'curriculo'))).toBeNull()
  })
})

describe('errosDoProblema', () => {
  test('leva as chaves do backend, em PascalCase, para os campos do formulário', () => {
    const erros = errosDoProblema({
      NomeCompleto: ['Informe o nome completo.'],
      Telefone: ['Informe um telefone com DDD, como (41) 99999-8888.'],
    })

    expect(erros).toEqual({
      campos: {
        nomeCompleto: 'Informe o nome completo.',
        telefone: 'Informe um telefone com DDD, como (41) 99999-8888.',
      },
      gerais: [],
    })
  })

  test('chave que não é campo do formulário vira mensagem geral', () => {
    const erros = errosDoProblema({ '$.email': ['The JSON value could not be converted.'] })

    expect(erros).toEqual({ campos: {}, gerais: ['The JSON value could not be converted.'] })
  })
})
