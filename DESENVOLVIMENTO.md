# Desenvolvimento

## Organização do trabalho

## Decisões técnicas

- **E-mail único por candidato.** Índice único na coluna `Email`. Não está no enunciado;
  foi adicionado para evitar candidato duplicado, principalmente ao importar o mesmo PDF duas vezes.
- **Connection string em user-secrets.** É o padrão do .NET para segredos em desenvolvimento:
  um comando, sem código extra e sem credenciais nos arquivos versionados.
- **Migrations aplicadas automaticamente só em Development.** Menos passos para quem avalia:
  basta rodar a API. Em produção, as migrations seriam aplicadas no deploy.
- **`CriadoEm` como `DateTimeOffset`.** Com `DateTime`, a data sairia no JSON sem fuso e o
  navegador mostraria a hora errada. Assim ela chega em UTC explícito.
- **Controller acessa o DbContext direto, sem camada de serviço.** São três operações simples
  sem regra repetida; os testes de integração cobrem de ponta a ponta, e um serviço só
  acrescentaria classes. Pode entrar quando a extração do PDF precisar compartilhar regras.
- **DTOs separados da entidade.** Entrada sem `Id`/`CriadoEm`; na saída, lista resumida
  (sem telefone e resumo) e detalhe completo, para o contrato da API não ser a entidade do EF.
- **Validação com DataAnnotations e mensagens em português.** Os tamanhos máximos vêm de
  constantes na entidade, usadas também pelo DbContext, para validação e colunas não divergirem.
- **Regex própria para e-mail** (`^[^\s@]+@([^\s@.]+\.)+[^\s@.]{2,}$`) em vez do `[EmailAddress]`,
  que só exige um `@` e aceita `a@b` ou `a@.com`. A mesma expressão serve no frontend.
- **Normalização no próprio DTO:** trim em todos os textos, vazio vira `null` e e-mail em
  minúsculas. Fica no `init` das propriedades porque a validação roda antes da action.
- **E-mail duplicado detectado pelo índice único (409), sem consulta prévia.** Uma consulta
  antes do insert não protege contra duas requisições simultâneas; o índice protege.
- **Erros no formato ProblemDetails**, com títulos em português configurados num só lugar
  (`CustomizeProblemDetails`); erro não tratado vira 500 sem stack trace.
- **Testes de integração com Testcontainers (SQL Server real)** em vez de SQLite ou InMemory,
  que se comportam diferente no índice único e na collation (o 409 por maiúsculas depende dela).
- **Connection string lida ao criar o DbContext, com checagem logo após o `Build()`.** A leitura
  tardia deixa os testes substituírem a dos user-secrets; a checagem faz a API falhar na subida
  em qualquer ambiente se ela faltar.

## Uso de IA

## Verificação

## Tempo dedicado

## Limitações

- Fora de Development as migrations não são aplicadas ao iniciar a API; é preciso rodar
  `dotnet ef database update` (ou gerar um script SQL) antes.
- A listagem não tem paginação: devolve todos os candidatos de uma vez.
- A regex de e-mail aceita alguns endereços inválidos na parte local (`a..b@x.com`, `.a@x.com`)
  e rejeita formatos válidos mas raros (`user@localhost`, IP entre colchetes, parte local entre aspas).
- JSON malformado ou com tipo errado (ex.: número no nome) retorna 400 com a mensagem padrão
  do .NET, em inglês.
- A unicidade do e-mail sem diferenciar maiúsculas depende da collation case-insensitive do
  banco (padrão do SQL Server); o e-mail também é gravado em minúsculas.
- O teste do erro 500 roda fora de Development (em Development a API tentaria migrar um banco
  inexistente ao subir); o mesmo tratamento vale para os dois ambientes, mas só um é testado.

## Dificuldades e melhorias
