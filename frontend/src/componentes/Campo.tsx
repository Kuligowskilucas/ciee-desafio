import type { ChangeEvent } from 'react'

interface CampoProps {
  id: string
  rotulo: string
  valor: string
  erro?: string
  obrigatorio?: boolean
  tipo?: 'text' | 'email' | 'tel'
  multilinha?: boolean
  aoAlterar: (valor: string) => void
}

export function Campo({ id, rotulo, valor, erro, obrigatorio, tipo = 'text', multilinha, aoAlterar }: CampoProps) {
  const idDoErro = `${id}-erro`
  const atributos = {
    id,
    name: id,
    value: valor,
    required: obrigatorio,
    'aria-invalid': erro ? true : undefined,
    'aria-describedby': erro ? idDoErro : undefined,
    onChange: (evento: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => aoAlterar(evento.target.value),
  }

  return (
    <div className="campo">
      <label htmlFor={id}>
        {rotulo}
        {obrigatorio && <span aria-hidden="true"> *</span>}
      </label>
      {multilinha ? <textarea rows={5} {...atributos} /> : <input type={tipo} {...atributos} />}
      {erro && (
        <p id={idDoErro} className="erro-do-campo">
          {erro}
        </p>
      )}
    </div>
  )
}
