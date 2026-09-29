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

## Uso de IA

## Verificação

## Tempo dedicado

## Limitações

- Fora de Development as migrations não são aplicadas ao iniciar a API; é preciso rodar
  `dotnet ef database update` (ou gerar um script SQL) antes.

## Dificuldades e melhorias
