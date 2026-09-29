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

## Pré-requisitos

- .NET SDK 10
- Docker com Docker Compose (também é usado pelos testes)
- Node.js 20.19+ ou 22.12+, com npm (para o frontend)

## 1. Subir o SQL Server

```bash
cp .env.example .env
```

Edite o `.env` e defina `SA_PASSWORD` com uma senha forte (mínimo de 8 caracteres, com
maiúsculas, minúsculas, números e símbolos; senhas fracas fazem o container não subir).

```bash
docker compose up -d
docker compose ps        # aguarde o status "healthy"
```

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
dotnet tool restore
dotnet ef database update --project backend/src/Candidatos.Api
```

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

Com a API rodando (passo 4), em outro terminal:

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
mesmo proxy. Para publicar em outro servidor, ele precisa repassar `/api` para a API.

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

Não precisa da API nem do banco: as chamadas ao `fetch` são simuladas nos testes.

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
