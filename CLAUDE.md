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
  - Nomes do domínio em português (Candidato, NomeCompleto, Telefone, AreaInteresse);
  sufixos e termos técnicos no padrão do .NET (Controller, DbContext, Service, Dto).
  - Toda etapa com regra de negócio (validação, extração do PDF, endpoints) inclui testes.
  - Ao fim de cada etapa, atualize as seções "Estrutura atual" e "Comandos" deste arquivo.

## Stack

- Backend: ASP.NET Core Web API (.NET 10) com controllers.
- Banco: SQL Server 2022 via `docker-compose.yml` na raiz, com EF Core e migrations.
- Testes: xUnit no backend.
- Frontend: React + TypeScript + Vite, em `frontend/` (ainda não criado).

## Estrutura atual

```
docker-compose.yml          SQL Server 2022 (container ciee-sqlserver, porta 1433)
.env.example                variáveis do compose (SA_PASSWORD); o .env real não é versionado
dotnet-tools.json           ferramentas locais do .NET (dotnet-ef)
docs/DESAFIO.md             enunciado
README.md                   como configurar, executar e testar do zero
DESENVOLVIMENTO.md          relato do desenvolvimento (decisões técnicas e limitações mantidas aqui)
backend/
  Candidatos.slnx           solution
  src/Candidatos.Api/       Web API
    Program.cs              DI, ProblemDetails (títulos em português), exception handler, checagem da connection string
    Controllers/            CandidatosController (POST, GET lista, GET por id)
    Dtos/                   CriarCandidatoDto (validação + normalização), CandidatoDto, CandidatoResumoDto
    Entities/               entidades do domínio (Candidato, com constantes de tamanho máximo)
    Validacao/              TelefoneBrasileiro (formato aceito e normalização para "(41) 99999-8888")
    Data/                   CandidatosDbContext (tamanhos, índices, defaults via Fluent API)
    Migrations/             migrations do EF Core (geradas, não editar à mão)
    Candidatos.Api.http     exemplos de requisições
  tests/Candidatos.Api.Tests/  testes xUnit de integração
    ApiFixture.cs           SQL Server via Testcontainers + WebApplicationFactory (collection fixture)
    CandidatosEndpointsTests.cs  cadastro, validação, 409, listagem, detalhe, 404
    TelefoneBrasileiroTests.cs   unitários da normalização do telefone
    ErroNaoTratadoTests.cs  500 em ProblemDetails sem stack trace
```

## Comandos

```bash
cp .env.example .env                        # depois definir SA_PASSWORD (senha forte)
docker compose up -d                        # sobe o SQL Server
docker compose ps                           # esperar status healthy

dotnet tool restore                         # instala o dotnet-ef local
dotnet user-secrets set "ConnectionStrings:Candidatos" \
  "Server=localhost,1433;Database=Candidatos;User Id=sa;Password=<SA_PASSWORD>;TrustServerCertificate=True" \
  --project backend/src/Candidatos.Api

dotnet build backend/Candidatos.slnx
dotnet test backend/Candidatos.slnx          # precisa do Docker rodando (Testcontainers)
dotnet run --project backend/src/Candidatos.Api   # http://localhost:5290; em Development aplica as migrations

dotnet ef migrations add <Nome> --project backend/src/Candidatos.Api
dotnet ef database update --project backend/src/Candidatos.Api
dotnet ef migrations has-pending-model-changes --project backend/src/Candidatos.Api
```

## Regras de trabalho

- Não escreva comentários no código (nem XML doc comments). O código deve se explicar
  por bons nomes de classes, métodos e variáveis.
- Quando usar um conceito de .NET novo para mim (injeção de dependência, DbContext,
  middleware, etc.), explique em 2-3 linhas só na resposta do terminal, comparando
  com Laravel quando fizer sentido. Essas explicações nunca vão para os arquivos.
- Trabalhe em etapas pequenas. Antes de implementar, apresente o plano e espere aprovação.
- Não adicione requisitos, bibliotecas ou abstrações que o desafio não pede sem
  justificar. Prefira a solução simples.
- Não faça commits. Ao fim de cada etapa, sugira a mensagem no padrão
  Conventional Commits, em português.
- Nunca coloque credenciais reais em arquivos versionados.
- Ao terminar uma etapa, resuma o que mudou e como eu verifico que funciona.
- Ao fim de cada etapa que mude pré-requisitos, configuração ou comandos, atualize o
  README.md (tecnologias e versões, configuração do banco e da connection string,
  criação da estrutura, execução e testes). Ele deve funcionar para quem clona do zero.
- Ao fim de cada etapa, registre no DESENVOLVIMENTO.md, nas seções "Decisões técnicas"
  e "Limitações", as decisões tomadas e seus motivos (1-3 linhas cada). Não escreva
  as seções sobre organização do trabalho, uso de IA, verificação e tempo: essas são minhas.
