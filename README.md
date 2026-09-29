# Cadastro de currículos — Desafio CIEE/PR

Cadastro de candidatos com importação opcional de currículo em PDF.
O relato do desenvolvimento está em [DESENVOLVIMENTO.md](DESENVOLVIMENTO.md).

## Tecnologias e versões

| Item | Versão |
|---|---|
| .NET SDK | 10.0.112 |
| ASP.NET Core Web API (controllers) | .NET 10 |
| Entity Framework Core (SqlServer, Design, dotnet-ef) | 10.0.12 |
| SQL Server | imagem `mcr.microsoft.com/mssql/server:2022-latest` (testado com 16.0.4295.3) |
| xUnit | 2.9.3 |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.12 |
| Testcontainers.MsSql | 4.15.0 |
| Docker + Docker Compose | qualquer versão recente com `docker compose` |

O frontend (React + TypeScript + Vite) ainda não foi criado.

## Pré-requisitos

- .NET SDK 10
- Docker com Docker Compose (também é usado pelos testes)

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

## 4. Executar

```bash
dotnet run --project backend/src/Candidatos.Api
```

A API sobe em `http://localhost:5290`. Endpoints:

| Método | Rota | Descrição |
|---|---|---|
| POST | `/api/candidatos` | cadastra um candidato (201; 400 se inválido; 409 se o e-mail já existe) |
| GET | `/api/candidatos` | lista resumida, mais recentes primeiro |
| GET | `/api/candidatos/{id}` | detalhes (404 se não existir) |

Exemplos prontos em `backend/src/Candidatos.Api/Candidatos.Api.http`. Erros seguem o formato
ProblemDetails (`application/problem+json`).

## 5. Testar

O Docker precisa estar rodando. Os testes de integração sobem um SQL Server próprio em
container (Testcontainers) e aplicam as migrations nele; não usam o banco do `docker compose`
nem os user-secrets. A primeira execução pode demorar enquanto baixa a imagem do SQL Server.

```bash
dotnet test backend/Candidatos.slnx
```
