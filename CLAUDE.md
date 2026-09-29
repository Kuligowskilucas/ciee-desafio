# Desafio CIEE/PR — Cadastro de currículos

Enunciado completo em `docs/DESAFIO.md`. Prazo: domingo, 04/10, 23h59.

## Requisitos obrigatórios

- Cadastro **manual** e cadastro **com PDF**, os dois usando o mesmo formulário e as mesmas validações.
- PDF: o backend extrai o texto e tenta achar nome, e-mail e telefone, que preenchem o formulário. O usuário pode corrigir ou completar antes de salvar.
- PDF é opcional: sem arquivo ou com falha na leitura, o cadastro manual continua funcionando.
- Campos: nome completo (obrigatório), e-mail (obrigatório, formato válido), telefone, área/cargo de interesse, resumo profissional.
- Listagem de candidatos com tela de detalhes.
- Arquivo aceito: só PDF de até 5 MB.
- Mensagens claras para arquivo inválido, falha na leitura e cadastro salvo.
- Persistência no SQL Server, estrutura criada por migrations.
- Testes relevantes.
- A entrega precisa conter:
  - `README.md`: tecnologias e versões, configuração da conexão, criação do banco, como executar e testar.
  - `DESENVOLVIMENTO.md`: organização do trabalho, decisões técnicas, uso de IA (ferramentas, modelos, exemplos de pedidos, o que foi corrigido ou descartado), verificação, tempo gasto, limitações e melhorias.
  - Exemplos de configuração sem credenciais reais.
  - Um currículo fictício em PDF para testar a importação.
  - Limitações da extração documentadas.
  - Histórico de commits que mostre a evolução do trabalho.

## Stack

- Backend: ASP.NET Core Web API (.NET 10) com controllers.
- Banco: SQL Server 2022 via `docker-compose.yml` na raiz, com EF Core e migrations.
- Docker: o mesmo `docker-compose.yml` sobe banco, API (imagem aspnet:10.0) e frontend (build
  estático servido por nginx:1.30-alpine, que repassa /api para a API). Só a porta 8080 do front
  é exposta, além da 1433 do banco.
- Testes: xUnit no backend.
- Leitura de PDF: PdfPig 0.1.16.
- Frontend: React 19 + TypeScript + Vite 8, em `frontend/`, com React Router 8 (modo declarativo),
  CSS próprio (`src/index.css`) e testes com Vitest + Testing Library; lint com oxlint.

## Estrutura atual

```
docker-compose.yml          sqlserver (container ciee-sqlserver, porta 1433, healthcheck), api (migrations na subida,
                            espera o banco healthy) e frontend (nginx na porta 8080)
.dockerignore               contexto de build na raiz, sem .env, .git, node_modules, bin, obj e dist
.env.example                variáveis do compose (SA_PASSWORD); o .env real não é versionado
dotnet-tools.json           ferramentas locais do .NET (dotnet-ef)
docs/DESAFIO.md             enunciado
README.md                   como configurar, executar e testar do zero
DESENVOLVIMENTO.md          relato do desenvolvimento (decisões técnicas e limitações mantidas aqui)
exemplos/                   currículos fictícios: normal, protegido por senha (ciee2026) e digitalizado; usados nos testes
casos-de-validacao.json     e-mails e telefones válidos/inválidos, lidos pelos testes do backend (xUnit) e do frontend (Vitest)
backend/
  Candidatos.slnx           solution
  Dockerfile                sdk:10.0 publica a API; aspnet:10.0 roda como usuário app, na porta 8080
  src/Candidatos.Api/       Web API
    Program.cs              DI, ProblemDetails (títulos em português), exception handler, checagem da connection string
    Controllers/            CandidatosController (POST, GET lista, GET por id), CurriculosController (POST extrair)
    Curriculos/             extração do PDF: ArquivoEnviado (leitura do upload com limite de 5 MB, só em memória),
                            LeitorPdf (PdfPig → texto), InterpretadorCurriculo (texto → nome, e-mail, telefone; puro)
    Dtos/                   CriarCandidatoDto (validação + normalização), CandidatoDto, CandidatoResumoDto, DadosCurriculoDto
    Entities/               entidades do domínio (Candidato, com constantes de tamanho máximo)
    Validacao/              TelefoneBrasileiro (formato aceito e normalização para "(41) 99999-8888")
    Data/                   CandidatosDbContext (tamanhos, índices, defaults via Fluent API)
    Migrations/             migrations do EF Core (geradas, não editar à mão)
    Candidatos.Api.http     exemplos de requisições
  tests/Candidatos.Api.Tests/  testes xUnit (integração e unitários)
    ApiFixture.cs           SQL Server via Testcontainers + WebApplicationFactory (collection fixture)
    CasosDeValidacao.cs     lê o casos-de-validacao.json para as theories de e-mail e telefone
    PdfDeTeste.cs           gera PDFs com o builder do PdfPig e lê os de exemplos/
    CandidatosEndpointsTests.cs  cadastro, validação, 409, listagem, detalhe, 404
    CurriculosEndpointsTests.cs  extração: sucesso, 400, 413 (inclusive com Kestrel real), 415, 422
    InterpretadorCurriculoTests.cs  unitários de nome, e-mail e telefone sobre textos (CPF, CEP, datas)
    LeitorPdfTests.cs       quebras de linha e separação de páginas
    ArquivoEnviadoTests.cs  falha se o formulário for lido antes, sem os limites
    TelefoneBrasileiroTests.cs   unitários da normalização do telefone
    ErroNaoTratadoTests.cs  500 em ProblemDetails sem stack trace
    MigrationsNaSubidaTests.cs  flag Migrations:AplicarAoIniciar fora de Development (cria ou não o banco)
frontend/                   React + TypeScript (Vite)
  vite.config.ts            proxy de /api para a API (5290) no dev e no preview; Vitest com jsdom
  Dockerfile                node:24-alpine faz o build; nginx:1.30-alpine serve o dist
  nginx.conf                fallback da SPA; /api repassado para api:8080 sem gravar o corpo em disco, até 28 MB
  src/
    main.tsx, App.tsx       BrowserRouter; cabeçalho e rotas (/candidatos, /candidatos/novo, /candidatos/:id)
    api.ts                  tipos dos DTOs, fetch com erros tipados (ErroDaApi, ApiIndisponivel), endpoints
    validacao.ts            regras iguais às do backend (e-mail, telefone, PDF) e mapeamento dos erros 400 para os campos
    formatacao.ts           data e hora em pt-BR
    componentes/            Campo (rótulo, input e erro acessível), ImportacaoPdf (validação do arquivo e extração)
    paginas/                ListaCandidatos, CadastroCandidato, DetalhesCandidato
    testes/setup.ts         jest-dom e limpeza entre testes
    *.test.ts(x)            Vitest + Testing Library com fetch simulado (validação, cadastro, importação, detalhes)
```

## Comandos

```bash
cp .env.example .env                        # depois definir SA_PASSWORD (senha forte)
docker compose up                           # caminho rápido: banco, API e front; http://localhost:8080
docker compose up --build                   # idem, reconstruindo as imagens depois de mudar o código
docker compose down                         # para tudo (-v apaga o volume do banco)

docker compose up -d sqlserver              # caminho manual: só o SQL Server
docker compose ps                           # esperar status healthy

dotnet tool restore                         # instala o dotnet-ef local
dotnet user-secrets set "ConnectionStrings:Candidatos" \
  "Server=localhost,1433;Database=Candidatos;User Id=sa;Password=<SA_PASSWORD>;TrustServerCertificate=True" \
  --project backend/src/Candidatos.Api

dotnet build backend/Candidatos.slnx
dotnet test backend/Candidatos.slnx          # precisa do Docker rodando (Testcontainers)
dotnet run --project backend/src/Candidatos.Api   # http://localhost:5290; em Development aplica as migrations
curl -F "arquivo=@exemplos/curriculo-ficticio.pdf" http://localhost:5290/api/curriculos/extrair

dotnet ef migrations add <Nome> --project backend/src/Candidatos.Api
dotnet ef database update --project backend/src/Candidatos.Api
dotnet ef migrations has-pending-model-changes --project backend/src/Candidatos.Api

cd frontend && npm install                  # dependências do frontend
npm run dev                                 # http://localhost:5173; precisa da API em http://localhost:5290
npm test                                    # Vitest (sem API nem banco)
npm run lint                                # oxlint
npm run build                               # checagem de tipos + build em frontend/dist
```

## Regras de trabalho

- Não escreva comentários no código (nem XML doc comments). O código deve se explicar
  por bons nomes de classes, métodos e variáveis.
- Nomes do domínio em português (Candidato, NomeCompleto, Telefone, AreaInteresse);
  sufixos e termos técnicos no padrão do .NET (Controller, DbContext, Service, Dto).
- Quando usar um conceito de .NET novo para mim (injeção de dependência, DbContext,
  middleware, etc.), explique em 2-3 linhas só na resposta do terminal, comparando
  com Laravel quando fizer sentido. Essas explicações nunca vão para os arquivos.
- Trabalhe em etapas pequenas. Antes de implementar, apresente o plano e espere aprovação.
- Toda etapa com regra de negócio (validação, extração do PDF, endpoints) inclui testes.
- Não adicione requisitos, bibliotecas ou abstrações que o desafio não pede sem
  justificar. Prefira a solução simples.
- Não faça commits. Ao fim de cada etapa, sugira a mensagem no padrão
  Conventional Commits, em português.
- Nunca coloque credenciais reais em arquivos versionados.
- Ao terminar uma etapa, resuma o que mudou e como eu verifico que funciona.
- Ao fim de cada etapa, atualize as seções "Estrutura atual" e "Comandos" deste arquivo.
- Ao fim de cada etapa que mude pré-requisitos, configuração ou comandos, atualize o
  README.md (tecnologias e versões, configuração do banco e da connection string,
  criação da estrutura, execução e testes). Ele deve funcionar para quem clona do zero.
- Ao fim de cada etapa, registre no DESENVOLVIMENTO.md, nas seções "Decisões técnicas"
  e "Limitações", as decisões tomadas e seus motivos (1-3 linhas cada). Não escreva
  as seções sobre organização do trabalho, uso de IA, verificação e tempo: essas são minhas.
