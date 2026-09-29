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
- **Telefone brasileiro com DDD, gravado sempre como `(41) 99999-8888` ou `(41) 3333-4444`.**
  Aceita os formatos comuns (com ou sem +55, parênteses, espaço, hífen, ponto ou só dígitos) e
  normaliza no `init`; o que não casa fica como veio e a validação recusa. Texto livre aceitava
  "abc" e gravaria o mesmo número de formas diferentes. A regra fica em `TelefoneBrasileiro`,
  para a extração do PDF usar a mesma.
- **E-mail duplicado detectado pelo índice único (409), sem consulta prévia.** Uma consulta
  antes do insert não protege contra duas requisições simultâneas; o índice protege.
- **Erros no formato ProblemDetails**, com títulos em português configurados num só lugar
  (`CustomizeProblemDetails`); erro não tratado vira 500 sem stack trace.
- **Testes de integração com Testcontainers (SQL Server real)** em vez de SQLite ou InMemory,
  que se comportam diferente no índice único e na collation (o 409 por maiúsculas depende dela).
- **Connection string lida ao criar o DbContext, com checagem logo após o `Build()`.** A leitura
  tardia deixa os testes substituírem a dos user-secrets; a checagem faz a API falhar na subida
  em qualquer ambiente se ela faltar.

### Extração do PDF

- **PdfPig 0.1.16** para ler o texto: .NET puro, sem dependência nativa, licença Apache 2.0.
  iText exigiria AGPL; Docnet depende do PDFium nativo.
- **`ContentOrderTextExtractor` em vez de `page.Text`.** Verificado: o `page.Text` junta as
  linhas ("MARIA DA SILVADesenvolvedora..."), e a regra do nome depende delas. As páginas são
  unidas com quebra de linha.
- **Leitura separada da interpretação.** `LeitorPdf` transforma bytes em texto;
  `InterpretadorCurriculo` transforma texto em nome, e-mail e telefone e é testado só com textos.
  São classes estáticas: não têm estado nem dependências, então não há o que injetar.
- **Formulário lido à mão em `ArquivoEnviado`, sem `IFormFile` e sem parâmetros na action.** Com
  qualquer parâmetro, o model binding lê o formulário antes, com os limites padrão: arquivo acima
  de 64 KB vai para um `.tmp` em disco, e o estouro de tamanho vira um 400 genérico em inglês.
  Se o formulário já tiver sido lido, `ArquivoEnviado` lança exceção em vez de seguir em silêncio.
- **Limite de 5 MB aplicado pelo FormOptions**, com `MultipartBodyLengthLimit` e
  `MemoryBufferThreshold` iguais a 5 MB: o arquivo fica só em memória e a leitura para no limite.
  Como quem para é o formulário, e não o Kestrel, o Kestrel descarta o resto do corpo e o 413
  chega ao cliente (medido até 29 MB). O 413 do próprio Kestrel também é traduzido, o que evita
  500 para clientes que usam `Expect: 100-continue`.
- **Estouro de tamanho e multipart malformado lançam a mesma `InvalidDataException`.** Para
  separar os dois, uso o `Content-Length`: acima de 5 MB é 413, senão é 400.
- **Status:** 400 quando falta o arquivo, 413 acima do tamanho, 415 quando o conteúdo não é PDF,
  422 quando é PDF mas não dá para ler (corrompido, com senha ou sem texto). O 422 separa
  "escolha outro arquivo" de "preencha à mão"; os três casos se distinguem pelo `detail`.
- **Só a assinatura `%PDF-` decide se é PDF.** Extensão e content-type são controlados por quem
  envia; um `.txt` renomeado é recusado e um PDF sem extensão é aceito.
- **Qualquer exceção do PdfPig, fora a de senha, vira "corrompido".** Com entrada quebrada, o
  parser lança vários tipos de exceção, e o arquivo é entrada não confiável.
- **Log sem dados pessoais:** numa falha de leitura, só o motivo (texto fixo) e o tipo da
  exceção. Nunca o texto extraído, os dados encontrados ou a mensagem da exceção.
- **Nome:** o primeiro trecho, nas 10 primeiras linhas, com 2 a 8 palavras só de letras e
  iniciais maiúsculas (conectivos como "da" à parte). Trechos com palavras de cabeçalho ou cargo
  ("Currículo", "Dados Pessoais", "Desenvolvedora") são pulados, e o rótulo "Nome:" é removido.
  É devolvido como está no documento.
- **E-mail:** o primeiro trecho em volta de um `@` que passa na mesma regex do cadastro, em
  minúsculas; o que a extração devolve sempre é aceito pelo formulário.
- **Telefone:** a mesma regra do cadastro, mas no texto do PDF uma sequência só de dígitos
  (`41999998888`) é ignorada, a não ser com +55: sem formatação, ela é igual a um CPF.
- **PDFs de `exemplos/`** gerados a partir do currículo fictício: com senha, pelo pypdf; o
  digitalizado, convertendo as páginas em imagem com pypdfium2 e Pillow. O builder do PdfPig não
  cria PDF com senha.
- **Um teste de integração com Kestrel real** (`UseKestrel`, novo no .NET 10), porque o
  TestServer não tem o limite de corpo do Kestrel nem descarta o corpo depois da resposta.

## Uso de IA

## Verificação

## Tempo dedicado

## Limitações

- Fora de Development as migrations não são aplicadas ao iniciar a API; é preciso rodar
  `dotnet ef database update` (ou gerar um script SQL) antes.
- A listagem não tem paginação: devolve todos os candidatos de uma vez.
- A regex de e-mail aceita alguns endereços inválidos na parte local (`a..b@x.com`, `.a@x.com`)
  e rejeita formatos válidos mas raros (`user@localhost`, IP entre colchetes, parte local entre aspas).
- O telefone só aceita números brasileiros com DDD: estrangeiros, prefixo de operadora
  (`0xx41`), ramal e celular antigo de 8 dígitos são recusados.
- JSON malformado ou com tipo errado (ex.: número no nome) retorna 400 com a mensagem padrão
  do .NET, em inglês.
- A unicidade do e-mail sem diferenciar maiúsculas depende da collation case-insensitive do
  banco (padrão do SQL Server); o e-mail também é gravado em minúsculas.
- O teste do erro 500 roda fora de Development (em Development a API tentaria migrar um banco
  inexistente ao subir); o mesmo tratamento vale para os dois ambientes, mas só um é testado.

### Extração do PDF

- **Layout em colunas ou tabelas:** o texto sai na ordem em que foi gravado no PDF, então colunas
  podem se misturar, e um nome numa coluna lateral pode ficar fora das 10 primeiras linhas.
- **PDF digitalizado (imagem) não é lido:** não há OCR. A API responde 422 e o cadastro segue
  manual.
- **Nome:** não reconhece nome todo em minúsculas, nome de uma palavra só nem partícula minúscula
  fora da lista (`d'Ávila`). Um bairro ou cidade numa linha própria antes do nome pode ser
  confundido com ele.
- **Nome em caixa alta** é devolvido como está ("MARIA DA SILVA"). Converter para "Maria da Silva"
  fica como melhoria.
- **E-mail** quebrado em duas linhas, ou escrito com espaços ou "[at]", não é reconhecido.
- **Telefone:** só brasileiro com DDD. No texto, uma sequência só de dígitos sem +55 é ignorada,
  e o travessão não é aceito como separador.
- **Só o primeiro** e-mail e o primeiro telefone do documento são usados.
- **Fontes sem mapa Unicode** podem gerar texto embaralhado, e aí nada é identificado.
- **Upload acima de ~28,6 MB** (limite padrão do Kestrel): a conexão é fechada durante o envio,
  e o navegador recebe um erro de rede, sem mensagem. O frontend precisa checar o tamanho antes.
- **Upload sem `Content-Length`** (chunked) acima de 5 MB recebe 400 em vez de 413. Navegadores
  sempre enviam o `Content-Length`.
- **`%PDF-` precisa estar no primeiro byte.** Leitores como o Acrobat toleram até 1 KB antes.
- **A leitura do PDF não tem tempo limite:** um PDF malicioso pode consumir CPU e memória.
- **"Não gravar em disco"** é garantido pela configuração e pela checagem em `ArquivoEnviado`,
  mas não tem teste automatizado: o `.tmp` some no fim da requisição.

## Dificuldades e melhorias
