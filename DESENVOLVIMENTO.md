# Desenvolvimento

## Organização do trabalho

Recebi o desafio na terça, 29/09, e fiz o desenvolvimento no mesmo dia. O plano inicial
ia até sábado, mas o ritmo permitiu terminar a parte principal antes; os dias seguintes
ficaram para revisão.

Comecei planejando no chat do Claude: stack, cronograma e ordem das etapas. Escolhi .NET
no backend por ser uma das opções da vaga e React no front, onde tenho mais experiência,
para concentrar o aprendizado no .NET. Minha base é Laravel e React.

A ordem foi:
1. Ambiente no WSL (.NET 10, Node, Docker) e SQL Server via docker compose.
2. EF Core, entidade Candidato e migration.
3. CRUD, validação e testes de integração.
4. Validação e normalização do telefone.
5. Extração do PDF.
6. Frontend, dividido em 6 passos: casos de validação compartilhados, projeto Vite,
   cliente da API e validações, tela de cadastro, lista e detalhes, documentação.

Em todas as etapas segui o mesmo ciclo: prompt com escopo e fora de escopo definidos,
perguntas do Claude Code sobre o que não estava no enunciado, plano revisado e aprovado
por mim, implementação, verificação e commit feito por mim. As regras desse ciclo estão
no `CLAUDE.md`, que mantive no repositório de propósito. Durante o trabalho mantive um
arquivo de notas, que é a base deste relato.

## Decisões técnicas

**Visão geral.** O backend é a fonte da verdade das regras de validação. O frontend repete só
as de e-mail e telefone, e o `casos-de-validacao.json`, lido pelos testes dos dois lados (xUnit
e Vitest), garante que elas continuem iguais. O telefone é aceito só como brasileiro com DDD e
gravado sempre como `(41) 99999-8888`, pela mesma regra no cadastro e na extração do PDF. A
extração separa a leitura do PDF (PdfPig) da interpretação do texto, uma classe pura testada só
com textos, e prefere deixar um campo vazio a preenchê-lo errado. O arquivo enviado fica só em
memória, o limite de 5 MB é aplicado na leitura do formulário, e todo problema com o arquivo
chega ao usuário como mensagem em português (400, 413, 415 ou 422), nunca como 500. Os testes
de integração rodam contra infraestrutura real: SQL Server via Testcontainers e, no limite de
upload, Kestrel real. No frontend, a escolha foi pelo simples: proxy do Vite em vez de CORS e
formulário com `useState`, sem biblioteca.

- **E-mail único por candidato.** Índice único na coluna `Email`. Não está no enunciado;
  foi adicionado para evitar candidato duplicado, principalmente ao importar o mesmo PDF duas vezes.
- **Connection string em user-secrets.** É o padrão do .NET para segredos em desenvolvimento:
  um comando, sem código extra e sem credenciais nos arquivos versionados.
- **Migrations aplicadas automaticamente em Development e no docker compose.** Menos passos para
  quem avalia: basta rodar a API ou o compose. Fora disso, só com a flag
  `Migrations:AplicarAoIniciar`; em produção, seriam aplicadas no deploy.
- **`CriadoEm` como `DateTimeOffset`.** Com `DateTime`, a data sairia no JSON sem fuso e o
  navegador mostraria a hora errada. Assim ela chega em UTC explícito.
- **Controller acessa o DbContext direto, sem camada de serviço.** São três operações simples,
  sem regra repetida, cobertas por testes de integração de ponta a ponta. A única regra que o
  cadastro e a extração do PDF compartilham, a do telefone, ficou na classe estática
  `TelefoneBrasileiro`, sem precisar de serviço no DI.
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
- **Nome todo em maiúsculas vira iniciais maiúsculas** ("MARIA DA SILVA" → "Maria da Silva"),
  com os conectivos da regra do nome (da, de, do, das, dos, e, di, du, del, van, von) em
  minúsculas. Um nome que já mistura maiúsculas e minúsculas fica como está no documento.
- **E-mail:** o primeiro trecho em volta de um `@` que passa na mesma regex do cadastro, em
  minúsculas; o que a extração devolve sempre é aceito pelo formulário.
- **Telefone:** a mesma regra do cadastro, mas no texto do PDF uma sequência só de dígitos
  (`41999998888`) é ignorada, a não ser com +55: sem formatação, ela é igual a um CPF.
- **PDFs de `exemplos/`** gerados a partir do currículo fictício: com senha, pelo pypdf; o
  digitalizado, convertendo as páginas em imagem com pypdfium2 e Pillow. O builder do PdfPig não
  cria PDF com senha.
- **Um teste de integração com Kestrel real** (`UseKestrel`, novo no .NET 10), porque o
  TestServer não tem o limite de corpo do Kestrel nem descarta o corpo depois da resposta.

### Frontend

- **Proxy do Vite em vez de CORS.** Em desenvolvimento, o Vite repassa `/api` para a API.
  O backend não muda, front e API ficam na mesma origem (sem preflight), e o código usa só
  caminhos relativos (`/api/...`).
- **Formulário com `useState` e funções de validação puras** (`validacao.ts`), sem biblioteca.
  São cinco campos; as regras são testadas sem React; os erros do front e os que vêm do backend
  ficam no mesmo objeto de erros.
- **React Router 8 no modo declarativo (`BrowserRouter`).** A URL é real: os detalhes abrem por
  link, e o voltar do navegador e o F5 funcionam.
- **CSS próprio num único arquivo**, sem dependência: a interface é simples.
- **Regras iguais às do backend.** As regex de e-mail e telefone estão copiadas em
  `validacao.ts`. O `casos-de-validacao.json`, lido pelo xUnit e pelo Vitest, faz os testes de
  um lado falharem se o outro mudar. No front, o telefone só é validado; a normalização fica no
  backend. Por isso a regex do front não usa grupos nomeados.
- **Mensagens de validação idênticas às do backend**, para o texto ser o mesmo tanto quando o
  erro é pego no front quanto quando vem da API.
- **O arquivo é validado no front pelo tamanho e pela assinatura `%PDF-`**, a mesma regra do
  backend: um PDF sem extensão passa e um `.txt` renomeado não. O `accept` do input só filtra o
  seletor de arquivos.
- **A importação começa assim que o arquivo é escolhido.** Os campos encontrados sobrescrevem
  os do formulário, e os não encontrados ficam como estão; a mensagem diz o que foi e o que não
  foi encontrado.
- **A falha na leitura do PDF nunca bloqueia o formulário.** Só o input de arquivo fica
  desabilitado durante a leitura. Em caso de erro, ele é limpo para permitir tentar o mesmo
  arquivo de novo.
- **"API fora do ar" = `fetch` rejeitado, ou 5xx sem `application/problem+json`.** O proxy do
  Vite responde 502 em texto quando a API está parada. Um 500 da própria API (em ProblemDetails)
  mostra "Ocorreu um erro no servidor".
- **Erros do backend no campo certo.** O 400 é mapeado para os campos (as chaves vêm em
  PascalCase, como `NomeCompleto`, e a comparação ignora maiúsculas); o 409 aparece no campo
  e-mail.
- **Formulário com `noValidate`**, para aparecerem as nossas mensagens, e não os balões do
  navegador.
- **Depois de salvar, a tela vai para os detalhes, com a mensagem passada no state da
  navegação.** O state é apagado do histórico (`navigate` com `replace`) assim que a mensagem é
  exibida, para ela não voltar no F5. Funciona como o flash da sessão no Laravel.
- **Requisições canceladas ao sair da tela** (`AbortController`). Isso evita resposta atrasada
  numa tela que já saiu e a requisição dupla do StrictMode em desenvolvimento.
- **Testes com `fetch` simulado (`vi.fn`), sem MSW.** Os testes de tela renderizam a `App` com
  `MemoryRouter` e encontram os elementos pelo papel e pelo rótulo acessível (`aria-invalid`,
  `aria-describedby`, `role="alert"` e `role="status"`).

### Docker

- **Um só `docker compose up` para banco, API e frontend.** O compose do SQL Server ganhou os
  serviços `api` e `frontend`. O caminho manual sobe só o banco (`docker compose up -d sqlserver`).
- **Frontend servido por nginx**, com fallback da SPA e `/api` repassado para a API. Isso resolve
  a limitação do proxy do Vite, que só existe no dev, com uma imagem pequena e uma config curta.
- **`proxy_request_buffering off` no `/api`.** Por padrão, o nginx grava corpos grandes num
  arquivo temporário; medido: um PDF de 1 MB foi para `client_temp`. Com o repasse direto, o PDF
  continua só em memória, e o limite de 5 MB segue na API (413 em português, medido com 7 MB).
- **`client_max_body_size 28m`:** acima dos 5 MB, para quem decide ser a API, e abaixo do limite
  do Kestrel (~28,6 MB), para ele nunca fechar a conexão no meio do upload.
- **Migrations na subida por flag (`Migrations__AplicarAoIniciar=true`)**, ligada só no compose:
  no container, a API continua em Production.
- **A API espera o banco com `depends_on: condition: service_healthy`**, usando o healthcheck
  que já existia no SQL Server, sem código de retry na aplicação.
- **Só a porta 8080 exposta, além da 1433.** A API é acessada pelo nginx, e o caminho rápido não
  disputa as portas do manual (5290 e 5173).
- **Contexto de build na raiz,** porque o `npm run build` checa os tipos dos testes, que leem o
  `casos-de-validacao.json`. O `.dockerignore` deixa de fora `.env`, `.git` e os artefatos.
- **Connection string numa variável de ambiente do compose,** montada com o `SA_PASSWORD` do
  `.env`, sem credencial versionada. A API roda com o usuário não root `app`, da imagem oficial.

## Uso de IA

**Ferramentas e modelos**
- Claude (claude.ai, modelo Claude Opus 5.5), num Projeto: planejamento, revisão dos
  planos e das respostas do Claude Code, e redação dos prompts.
- Claude Code (Claude Opus 5.5, effort high), no WSL: implementação, testes e as seções
  "Decisões técnicas" e "Limitações" deste arquivo.
- O texto destas seções (organização, uso de IA, verificação, tempo e melhorias) foi
  redigido com ajuda do Claude a partir das minhas notas e revisado por mim.

**Como conduzi**
Criei o `CLAUDE.md` com o enunciado, a stack e regras de trabalho, algumas acrescentadas
por mim: sem comentários no código, nomes do domínio em português, testes em toda regra
de negócio e nenhum commit feito pela IA. Nos prompts, pedi que qualquer decisão fora do enunciado
viesse como pergunta, com prós e contras, em vez de ser implementada direto.

**Exemplos de pedidos e como usei as respostas**
- Na etapa do banco, pedi opções com prós e contras para a connection string e para a
  aplicação das migrations. Escolhi user-secrets (padrão do .NET, sem código extra) e
  migration automática só em Development (menos passos para quem avalia).
- A extração do PDF acabou com uma leitura manual do formulário, que me pareceu complexa
  demais para "uma solução simples". Perguntei por que não usar `IFormFile` com
  `[RequestSizeLimit]`, pedindo só a comparação, sem mudar código. Ele mediu as duas
  opções: com os atributos, um arquivo grande vira erro de rede ou um 400 genérico em
  inglês. Mantive a solução, porque o enunciado pede mensagens claras para arquivo
  inválido, e pedi que a comparação fosse registrada nas decisões técnicas.
- No frontend, ele trouxe as opções em duas rodadas de perguntas (comunicação com a API,
  formulário, rotas, estilo, regras compartilhadas, comportamento do PDF, destino após
  salvar, mocks). Escolhi as mais simples que atendiam o enunciado e acrescentei duas
  exigências: validação em funções puras testáveis e os testes do backend também lendo
  o JSON de casos compartilhados.

**O que corrigi, recusei ou mudei**
- Recusei a recomendação de devolver o DTO completo na listagem. Escolhi um DTO resumido,
  para a API refletir as telas de lista e de detalhes.
- Pedi `CriadoEm` com fuso explícito: como `DateTime`, a data sairia sem fuso no JSON e
  o navegador mostraria a hora errada.
- Pedi que a API falhe na subida em qualquer ambiente se faltar a connection string
  (o plano tinha deixado isso só em Development) e títulos de erro em português.
- Pedi versões exatas no README (ele tinha posto só "10.0").
- O plano do frontend previa um único commit. Pedi 6, com pausa em cada um, para o
  histórico mostrar a evolução e para eu revisar em partes menores.
- A IA também errou. O chat indicou um nome de pacote que não existe no apt
  (`dotnet-sdk`, o certo é `dotnet-sdk-10.0`). Também previu que a mensagem "Cadastro
  salvo" sumiria ao dar F5 nos detalhes; no teste manual vi que ela continuava, porque
  o state da navegação fica no histórico do navegador, e pedi a correção.
- Deixei de fora extrair área de interesse e resumo do PDF: esses campos não têm padrão
  confiável no texto, e um campo preenchido errado é pior que um vazio.

**O que a IA fez bem sem eu pedir**
- Achou um bug sutil no upload: qualquer parâmetro na action fazia o ASP.NET ler o
  formulário antes, com os limites padrão, e a checagem de 5 MB deixava de funcionar
  sem nenhum teste falhar.

## Verificação

- **Testes automatizados:** 133 no backend (xUnit, com SQL Server real via Testcontainers)
  e 68 no frontend (Vitest + Testing Library). Lint e build sem avisos.
- **Regras compartilhadas:** o `casos-de-validacao.json` é lido pelos testes dos dois
  lados. Para provar que funciona, foi colocado de propósito um caso errado de cada
  lado, e os testes falharam; depois o arquivo foi restaurado.
- **Testes de mutação:** na extração, o código foi quebrado de propósito em três pontos,
  e os testes pegaram as três quebras.
- **API real:** os 7 cenários de upload testados com curl (PDF fictício, protegido,
  digitalizado, sem arquivo, .txt renomeado, 7 MB e 40 MB).
- **Teste manual no navegador, feito por mim:** validação dos campos, importação dos três
  PDFs de exemplo, e-mail duplicado, API parada, lista, detalhes e F5. Foi nesse teste
  que achei o problema da mensagem no F5.
- **Do zero:** [clonei o repositório numa pasta nova e segui o README; resultado: ...]
- Para entender o que estava sendo entregue, pedi explicação dos conceitos novos de .NET
  e React que apareceram, como injeção de dependência, `DbContext`, `[GeneratedRegex]`,
  a palavra-chave `field` do C# 14 e o estado derivado no lugar de `setState` no efeito.


## Tempo dedicado

Cerca de 8 horas e 40 minutos, todas na terça, 29/09:

| Etapa | Tempo |
|---|---|
| Planejamento e ambiente | 1h |
| Banco, entidade e migration | 1h30 |
| CRUD, validação e testes | 40 min |
| Telefone | 2h30 |
| Extração do PDF | 1h |
| Frontend e documentação | 3h |

## Limitações

- Fora de Development e do docker compose, as migrations não são aplicadas ao iniciar a API:
  é preciso rodar `dotnet ef database update` (ou gerar um script SQL) antes, ou ligar a flag
  `Migrations:AplicarAoIniciar`.
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
- **Na conversão de nome em caixa alta** não dá para saber a caixa original: "MCDONALD" vira
  "Mcdonald", e "PEDRO II" vira "Pedro Ii".
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

### Frontend

- **O proxy do Vite só existe no `npm run dev` e no `npm run preview`.** No docker compose, quem
  repassa o `/api` é o nginx. Em outro servidor, seria preciso fazer o mesmo, ou liberar CORS no
  backend.
- **As regex do front são cópias das do backend.** O arquivo de casos só garante que as duas
  concordam nos casos listados nele.
- **Os tamanhos máximos dos campos só são conferidos no backend:** o erro aparece no campo
  depois do envio.
- **Não há** máscara de telefone, paginação, busca, edição nem exclusão de candidatos.
- **A lista e os detalhes não têm testes próprios**, além do teste da mensagem de sucesso nos
  detalhes. Não há testes ponta a ponta num navegador de verdade: os testes do front usam jsdom
  e `fetch` simulado.
- **A data de cadastro aparece no fuso do navegador.**

### Docker

- **Acima de 28 MB, pela porta 8080, quem responde é o nginx,** com um 413 em HTML, e não a nossa
  mensagem. O frontend barra qualquer arquivo acima de 5 MB antes do envio.
- **A API conecta como `sa`.** Em produção, teria um usuário próprio só com as permissões
  necessárias.
- **A espera pelo banco só vale na subida.** Se o SQL Server reiniciar depois, as requisições
  falham até ele voltar, e a API não tenta de novo.
- **A primeira execução baixa as imagens e faz os builds,** o que leva alguns minutos.
- **O container do banco tem nome fixo (`ciee-sqlserver`) e usa a porta 1433,** então só uma
  cópia do projeto sobe por vez na mesma máquina.

## Dificuldades e melhorias

**Dificuldades**
- .NET era a parte nova para mim. O que mais exigiu entendimento foi a configuração
  (user-secrets, leitura tardia da connection string nos testes) e o tratamento do
  upload, em que o model binding do ASP.NET lê o formulário antes da action.
- Os limites de upload têm comportamentos diferentes no Kestrel, no formulário e no
  navegador. Foi preciso medir cada caso para escolher a solução.
- O Claude Code não conseguiu abrir o navegador no WSL, então a verificação das telas
  foi manual.

**Melhorias com mais tempo**
- Sugerir área de interesse e resumo a partir de seções como "Objetivo" e "Resumo" do
  currículo, com a pessoa confirmando antes de salvar.
- OCR para PDFs digitalizados.
- Paginação e busca na listagem; edição e exclusão de candidatos.
- Testes ponta a ponta num navegador real (Playwright).
