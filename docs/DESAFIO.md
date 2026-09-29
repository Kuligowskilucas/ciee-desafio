Desafio técnico — Cadastro de currículos

Queremos conhecer sua forma de resolver problemas, organizar o código e utilizar as ferramentas disponíveis.

O uso de inteligência artificial é permitido, desde que você documente como ela participou do desenvolvimento e consiga explicar a solução entregue.

Imagine que nossa equipe de recrutamento precisa cadastrar e consultar candidatos. Sua aplicação deve oferecer duas formas de cadastro:

Cadastro manual: a pessoa preenche o formulário e salva os dados, sem precisar enviar um documento.
Cadastro com PDF: a pessoa envia um currículo, e a aplicação extrai o texto e tenta identificar nome, e-mail e telefone. As informações encontradas preenchem o formulário e podem ser corrigidas ou complementadas antes de salvar.
Nos dois caminhos, utilize o mesmo formulário e as mesmas regras de validação. Depois de salvar, o candidato deve aparecer em uma listagem, com acesso a uma tela de detalhes.

O PDF é opcional. A ausência do arquivo ou uma falha na leitura não pode impedir o cadastro manual.

Dados do cadastro

Nome completo — obrigatório.
E-mail — obrigatório.
Telefone.
Área ou cargo de interesse.
Resumo profissional.
Tecnologias obrigatórias

Frontend: Angular ou React.
Backend: ASP.NET Core (.NET) ou Node.js.
Banco de dados: SQL Server.
Escolha uma das opções de frontend e uma de backend. As demais bibliotecas ficam a seu critério. Informe no README as tecnologias e versões utilizadas.

Requisitos da aplicação

Interface simples e funcional, integrada ao backend.
Leitura do PDF realizada pelo backend.
Cadastro e consulta dos dados por meio do backend, com persistência no SQL Server.
Scripts ou migrations para criar a estrutura do banco.
Validação dos campos obrigatórios e do formato do e-mail.
Validação do arquivo enviado, aceitando PDF de até 5 MB.
Mensagens claras para situações como arquivo inválido, falha na leitura e cadastro salvo.
Não esperamos que a extração funcione perfeitamente com qualquer currículo. Quando uma informação não for identificada, o formulário deve permitir seu preenchimento manual. Documente as limitações da sua solução.

Registro do desenvolvimento e uso de IA

Inclua um arquivo DESENVOLVIMENTO.md explicando:

Como você organizou e executou o trabalho.
As principais decisões técnicas e seus motivos.
Quais ferramentas de IA e modelos utilizou, se houver.
Em quais etapas a IA ajudou, com alguns exemplos de pedidos e como as respostas foram aproveitadas.
O que você precisou corrigir, adaptar ou descartar.
Como verificou se a solução estava correta.
O tempo aproximado dedicado ao desafio.
As dificuldades, limitações e melhorias que faria com mais tempo.
Se não utilizar IA, registre isso e descreva seu processo normalmente. Não precisamos do histórico completo das conversas; queremos entender sua participação e suas decisões.

Prazo e dedicação

Você terá até domingo, dia 04/10, às 23h59, para entregar o desafio, respondendo a este e-mail com o link do repositório no GitHub.

Caso não consiga concluir tudo, entregue o que desenvolveu e registre as pendências e os próximos passos. Sua organização e capacidade de priorizar também serão consideradas.

Entrega

Responda a este e-mail com o link do repositório no GitHub, acessível para avaliação.

O repositório deve conter:

Código-fonte do frontend e do backend.
README.md com os requisitos e comandos para configurar a conexão com o SQL Server, criar a estrutura do banco, executar a aplicação e rodar os testes.
Exemplos de configuração sem credenciais reais.
Scripts ou migrations do banco de dados.
DESENVOLVIMENTO.md com o relato do desenvolvimento.
Um currículo fictício em PDF para testar a importação.
Histórico de commits que acompanhe a evolução do trabalho.
Critérios de avaliação

Vamos observar:

Funcionamento dos cadastros manual e com PDF, da listagem e da consulta de detalhes.
Integração entre frontend, backend e SQL Server.
Clareza e organização do código.
Validações e tratamento de erros.
Relevância dos testes.
Facilidade para configurar e executar o projeto.
Clareza na documentação e capacidade de explicar as decisões.
Prefira uma solução simples, funcional e que você consiga compreender e evoluir.

Se tiver alguma dúvida sobre a vaga ou o desafio, responda a este e-mail.

Bom desafio!

Equipe CIEE/PR
