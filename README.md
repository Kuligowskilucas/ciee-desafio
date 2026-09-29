# Cadastro de currículos — Desafio CIEE/PR

Cadastro de candidatos com importação opcional de currículo em PDF: API em ASP.NET Core com
SQL Server (`backend/`) e interface em React (`frontend/`).
O relato do desenvolvimento está em [DESENVOLVIMENTO.md](DESENVOLVIMENTO.md).

## Tecnologias e versões

| Item | Versão |
|---|---|
| .NET SDK | 10.0.112 |
| ASP.NET Core Web API (controllers) | .NET 10 |
| Entity Framework Core (SqlServer, Design, dotnet-ef) | 10.0.12 |
| PdfPig (leitura do texto dos PDFs) | 0.1.16 |
| SQL Server | imagem `mcr.microsoft.com/mssql/server:2022-latest` (testado com 16.0.4295.3) |
| xUnit | 2.9.3 |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.12 |
| Testcontainers.MsSql | 4.15.0 |
| Docker + Docker Compose | qualquer versão recente com `docker compose` |
| Node.js / npm | 20.19+ ou 22.12+ (testado com Node 24.18.0 e npm 11.16.0) |
| React / React DOM | 19.3.0 |
| TypeScript | 6.0.3 |
| Vite (+ @vitejs/plugin-react) | 8.3.1 (6.1.1) |
| React Router | 8.4.0 |
| Vitest / jsdom | 5.0.2 / 30.1.1 |
| Testing Library (react, user-event, jest-dom) | 16.3.3, 14.6.7, 7.0.1 |
| oxlint | 1.86.0 |
| Imagens do caminho com Docker | `mcr.microsoft.com/dotnet/sdk:10.0` e `aspnet:10.0` (API), `node:24-alpine` (build do front), `nginx:1.30-alpine` (serve o front) |

## Pré-requisitos

**Caminho rápido (tudo com Docker):** só Git e Docker com Docker Compose. Não é preciso ter o
.NET SDK nem o Node instalados.

**Caminho manual (passos 1 a 6, para desenvolver e rodar os testes):**

- .NET SDK 10
- Docker com Docker Compose (também é usado pelos testes)
- Node.js 20.19+ ou 22.12+, com npm (para o frontend)
- Git

Portas livres: 1433 (SQL Server, nos dois caminhos), 8080 (caminho rápido), 5290 (API) e
5173 (frontend) do caminho manual. Se já houver um SQL Server local na 1433, pare-o antes.

## 0. Clonar o repositório

```bash
git clone https://github.com/Kuligowskilucas/ciee-desafio.git
cd ciee-desafio
```

Todos os comandos deste README rodam a partir dessa pasta, a raiz do repositório, exceto
quando o passo indicar outra pasta (como `cd frontend`). Eles estão em sintaxe de bash (Linux,
macOS, WSL ou Git Bash); no PowerShell, a continuação de linha com `\` não funciona.

## Caminho rápido: tudo com Docker

O único pré-requisito é o Docker com Docker Compose: o banco, a API e o frontend sobem em
containers, sem .NET SDK nem Node instalados na máquina.

```bash
cp .env.example .env
```

Edite o `.env` e defina `SA_PASSWORD` com uma senha forte (as regras estão no passo 1 abaixo;
evite `$`, `!`, `;` e aspas). Depois:

```bash
docker compose up
```

A primeira execução demora alguns minutos: o Docker baixa as imagens (SQL Server, SDK e runtime
do .NET, Node e nginx) e faz o build da API e do frontend. As seguintes usam o cache.

Quando os três serviços estiverem no ar, abra http://localhost:8080.

- A API aplica as migrations ao subir, depois que o SQL Server fica saudável.
- A API não é exposta diretamente: o nginx do frontend repassa `/api` para ela. Os exemplos com
  curl usam a porta 8080, por exemplo
  `curl -F "arquivo=@exemplos/curriculo-ficticio.pdf" http://localhost:8080/api/curriculos/extrair`.
- Para parar: `Ctrl+C` ou `docker compose down`. Os dados ficam no volume `sqlserver-data`
  (`docker compose down -v` apaga).
- Depois de mudar o código: `docker compose up --build`.

Os passos 1 a 6 abaixo são o caminho manual, que roda cada parte direto na máquina.

## 1. Subir o SQL Server

```bash
cp .env.example .env
```

Edite o `.env` e defina `SA_PASSWORD` com uma senha forte (mínimo de 8 caracteres, com
maiúsculas, minúsculas, números e símbolos; senhas fracas fazem o container não subir).
Use símbolos como `@`, `#`, `%` ou `_`: `$` e `!` são interpretados pelo shell no passo 2 (e o
`$` também pelo Docker Compose no `.env`), e `;` ou aspas quebram a connection string.

```bash
docker compose up -d sqlserver
docker compose ps        # aguarde o status "healthy"
```

Aqui sobe só o banco. Sem o nome do serviço, o `docker compose up` sobe também a API e o frontend
em containers (caminho rápido).

## 2. Configurar a connection string

A connection string não fica em arquivos versionados. Em desenvolvimento, ela é
guardada com user-secrets:

```bash
dotnet user-secrets set "ConnectionStrings:Candidatos" \
  "Server=localhost,1433;Database=Candidatos;User Id=sa;Password=<SA_PASSWORD>;TrustServerCertificate=True" \
  --project backend/src/Candidatos.Api
```

Troque `<SA_PASSWORD>` pela senha definida no `.env`. Como alternativa, use a variável
de ambiente `ConnectionStrings__Candidatos` com o mesmo valor.

## 3. Criar a estrutura do banco

A estrutura é criada por migrations do EF Core (`backend/src/Candidatos.Api/Migrations`).

Ao rodar a API em Development (passo 4), as migrations pendentes são aplicadas
automaticamente. Para aplicá-las manualmente:

```bash
dotnet build backend/Candidatos.slnx
dotnet tool restore
dotnet ef database update --project backend/src/Candidatos.Api
```

O `dotnet build` restaura os pacotes NuGet da solution. Num clone novo, sem ele, o `dotnet ef`
falha com `NETSDK1004` (`project.assets.json` não encontrado). O `dotnet tool restore` instala o
`dotnet-ef` na versão fixada em `dotnet-tools.json`.

## 4. Executar a API

```bash
dotnet run --project backend/src/Candidatos.Api
```

A API sobe em `http://localhost:5290`. Endpoints:

| Método | Rota | Descrição |
|---|---|---|
| POST | `/api/candidatos` | cadastra um candidato (201; 400 se inválido; 409 se o e-mail já existe) |
| GET | `/api/candidatos` | lista resumida, mais recentes primeiro |
| GET | `/api/candidatos/{id}` | detalhes (404 se não existir) |
| POST | `/api/curriculos/extrair` | lê um currículo em PDF e devolve nome, e-mail e telefone encontrados (não salva nada) |

Exemplos prontos em `backend/src/Candidatos.Api/Candidatos.Api.http`. Erros seguem o formato
ProblemDetails (`application/problem+json`), com a mensagem para o usuário em `detail`.

### Importação de currículo em PDF

O arquivo vai no campo `arquivo` de um `multipart/form-data`:

```bash
curl -F "arquivo=@exemplos/curriculo-ficticio.pdf" http://localhost:5290/api/curriculos/extrair
```

```json
{ "nomeCompleto": "Mariana Alves Ferreira", "email": "mariana.ferreira@example.com", "telefone": "(41) 98765-4321" }
```

O que não for identificado volta como `null`. Respostas de erro:

| Status | Situação |
|---|---|
| 400 | nenhum arquivo enviado (ou formulário malformado) |
| 413 | arquivo com mais de 5 MB (5 × 1024 × 1024 bytes) |
| 415 | o conteúdo não é PDF (a assinatura `%PDF-` é conferida; extensão e content-type não) |
| 422 | PDF corrompido, protegido por senha ou sem texto selecionável (digitalizado) |

A pasta `exemplos/` tem currículos fictícios para testar a importação (também usados nos testes):

| Arquivo | Resultado esperado |
|---|---|
| `curriculo-ficticio.pdf` | 200 com nome, e-mail e telefone |
| `curriculo-protegido.pdf` | 422, protegido por senha (a senha é `ciee2026`) |
| `curriculo-digitalizado.pdf` | 422, sem texto selecionável (é o mesmo currículo convertido em imagem) |

## 5. Executar o frontend

Com a API rodando (passo 4), abra outro terminal na raiz do repositório e rode:

```bash
cd frontend
npm install
npm run dev
```

Abra http://localhost:5173. Telas: lista de candidatos (`/candidatos`), cadastro com importação
opcional de PDF (`/candidatos/novo`) e detalhes (`/candidatos/{id}`).

Em desenvolvimento, o Vite repassa as chamadas a `/api` para `http://localhost:5290` (proxy em
`frontend/vite.config.ts`), então a API não precisa de CORS. Se a API estiver parada, a interface
mostra "Não foi possível conectar à API".

`npm run build` gera a versão de produção em `frontend/dist`, e `npm run preview` a serve com o
mesmo proxy. Fora do Vite, o servidor precisa repassar `/api` para a API: é o que o nginx do
caminho rápido faz (`frontend/nginx.conf`).

## 6. Testar

### Backend

O Docker precisa estar rodando. Os testes de integração sobem um SQL Server próprio em
container (Testcontainers) e aplicam as migrations nele; não usam o banco do `docker compose`
nem os user-secrets. A primeira execução pode demorar enquanto baixa a imagem do SQL Server.
A interpretação do texto do currículo e a regra do telefone também têm testes unitários, que
não dependem de PDF nem de banco.

```bash
dotnet test backend/Candidatos.slnx
```

### Frontend

Não precisa da API nem do banco: as chamadas ao `fetch` são simuladas nos testes. A partir da
raiz do repositório (o `npm install` do passo 5 precisa ter sido feito):

```bash
cd frontend
npm test          # Vitest + Testing Library
npm run lint      # oxlint
npm run build     # inclui a checagem de tipos do TypeScript
```

### Regras compartilhadas

`casos-de-validacao.json`, na raiz, lista e-mails e telefones válidos e inválidos. Os testes do
backend (xUnit) e do frontend (Vitest) leem o mesmo arquivo. Se a regra mudar só de um lado, os
testes do outro falham.
