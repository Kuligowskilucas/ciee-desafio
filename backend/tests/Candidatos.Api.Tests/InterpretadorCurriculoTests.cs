using Candidatos.Api.Curriculos;
using Candidatos.Api.Dtos;

namespace Candidatos.Api.Tests;

public class InterpretadorCurriculoTests
{
    [Fact]
    public void Interpretar_TextoDoCurriculoDeExemplo_EncontraOsTresCampos()
    {
        const string texto = """
            Mariana Alves Ferreira
            Desenvolvedora Back-end | .NET e SQL Server
            Curitiba, PR | (41) 98765-4321 | mariana.ferreira@example.com
            Rua das Araucárias, 1450, ap. 32 - Água Verde - CEP 80240-210 | Nascimento: 14/06/1996
            RESUMO PROFISSIONAL
            Desenvolvedora back-end com 6 anos de experiência em C# e ASP.NET Core.
            """;

        var dados = InterpretadorCurriculo.Interpretar(texto);

        Assert.Equal(new DadosCurriculoDto("Mariana Alves Ferreira", "mariana.ferreira@example.com", "(41) 98765-4321"), dados);
    }

    [Theory]
    [InlineData("Mariana Alves Ferreira\nDesenvolvedora Back-end", "Mariana Alves Ferreira")]
    [InlineData("CURRÍCULO\nMaria da Silva", "Maria da Silva")]
    [InlineData("Curriculum Vitae\n\nJoão Pedro dos Santos", "João Pedro dos Santos")]
    [InlineData("Currículo - Maria da Silva", "Maria da Silva")]
    [InlineData("DADOS PESSOAIS\nNome: João P. Santos\nIdade: 25 anos", "João P. Santos")]
    [InlineData("Nome completo: Ana-Maria D'Ávila", "Ana-Maria D'Ávila")]
    [InlineData("Nome:\nMaria Souza", "Maria Souza")]
    [InlineData("Desenvolvedora Full Stack | Maria Souza", "Maria Souza")]
    [InlineData("Maria   da   Silva  |  (41) 99999-8888", "Maria da Silva")]
    [InlineData("Currículo\nObjetivo Profissional\nMaria Souza", "Maria Souza")]
    public void Interpretar_NomeNoTopo_EncontraONome(string texto, string esperado)
    {
        Assert.Equal(esperado, InterpretadorCurriculo.Interpretar(texto).NomeCompleto);
    }

    [Theory]
    [InlineData("MARIA DA SILVA\nmaria@exemplo.com", "Maria da Silva")]
    [InlineData("JOÃO PEDRO DOS SANTOS", "João Pedro dos Santos")]
    [InlineData("ÉRICA ÁVILA E SOUZA", "Érica Ávila e Souza")]
    [InlineData("ANA-MARIA D'ÁVILA", "Ana-Maria D'Ávila")]
    [InlineData("JOÃO P. SANTOS", "João P. Santos")]
    [InlineData("ANA DI PIETRO", "Ana di Pietro")]
    [InlineData("LUCAS VAN HALEN", "Lucas van Halen")]
    [InlineData("CURRÍCULO\nNome: MARIA DAS DORES", "Maria das Dores")]
    public void Interpretar_NomeTodoEmMaiusculas_ConverteParaIniciaisMaiusculas(string texto, string esperado)
    {
        Assert.Equal(esperado, InterpretadorCurriculo.Interpretar(texto).NomeCompleto);
    }

    [Theory]
    [InlineData("Maria DA Silva")]
    [InlineData("JOÃO da Silva")]
    [InlineData("Carlos McDonald Souza")]
    public void Interpretar_NomeComMaiusculasEMinusculas_MantemComoEstaNoDocumento(string nome)
    {
        Assert.Equal(nome, InterpretadorCurriculo.Interpretar(nome).NomeCompleto);
    }

    [Theory]
    [InlineData("Maria\nmaria@exemplo.com")]
    [InlineData("maria da silva\nmaria@exemplo.com")]
    [InlineData("Maria da Silva de")]
    [InlineData("da Silva Maria")]
    [InlineData("Maria Silva 2024")]
    [InlineData("Experiência Profissional\nFormação Acadêmica")]
    [InlineData("Rua das Araucárias, 1450 - Água Verde\nCuritiba, PR")]
    [InlineData("Ana Beatriz Carolina Fernanda Gabriela Helena Isabela Juliana Larissa")]
    [InlineData("")]
    public void Interpretar_SemLinhaComCaraDeNome_RetornaNomeNull(string texto)
    {
        Assert.Null(InterpretadorCurriculo.Interpretar(texto).NomeCompleto);
    }

    [Theory]
    [InlineData(9, "Maria da Silva")]
    [InlineData(10, null)]
    public void Interpretar_SoProcuraONomeNasDezPrimeirasLinhas(int linhasAntes, string? esperado)
    {
        var texto = string.Join('\n', Enumerable.Range(1, linhasAntes).Select(numero => $"{numero}. atividade").Append("Maria da Silva"));

        Assert.Equal(esperado, InterpretadorCurriculo.Interpretar(texto).NomeCompleto);
    }

    [Theory]
    [InlineData("E-mail: Maria.Silva@Exemplo.com.br", "maria.silva@exemplo.com.br")]
    [InlineData("Contato: <maria@exemplo.com>", "maria@exemplo.com")]
    [InlineData("mailto:maria@exemplo.com", "maria@exemplo.com")]
    [InlineData("Escreva para maria@exemplo.com.", "maria@exemplo.com")]
    [InlineData("E-mail:maria@exemplo.com|(41) 99999-8888", "maria@exemplo.com")]
    [InlineData("maria@exemplo e joao@exemplo.com", "joao@exemplo.com")]
    [InlineData("Instagram @maria.silva - maria@exemplo.com", "maria@exemplo.com")]
    [InlineData("primeiro@exemplo.com\nsegundo@exemplo.com", "primeiro@exemplo.com")]
    public void Interpretar_RetornaPrimeiroEmailValidoEmMinusculas(string texto, string esperado)
    {
        Assert.Equal(esperado, InterpretadorCurriculo.Interpretar(texto).Email);
    }

    [Theory]
    [InlineData("Sem e-mail informado")]
    [InlineData("maria@exemplo")]
    [InlineData("@maria.silva")]
    [InlineData("maria @ exemplo.com")]
    public void Interpretar_SemEmailValido_RetornaEmailNull(string texto)
    {
        Assert.Null(InterpretadorCurriculo.Interpretar(texto).Email);
    }

    [Theory]
    [InlineData("Celular/WhatsApp: (41) 98765-4321 | maria@exemplo.com", "(41) 98765-4321")]
    [InlineData("Tel.: +55 41 3333-4444", "(41) 3333-4444")]
    [InlineData("Contato: 41.99999.8888", "(41) 99999-8888")]
    [InlineData("WhatsApp +5541999998888", "(41) 99999-8888")]
    [InlineData("(41) 9 9999-8888", "(41) 99999-8888")]
    [InlineData("CPF: 123.456.789-09\nCelular: (41) 99999-8888", "(41) 99999-8888")]
    [InlineData("CEP 80240-210 | Tel: 41 3333 4444", "(41) 3333-4444")]
    [InlineData("(41) 99999-8888 / (41) 3333-4444", "(41) 99999-8888")]
    public void Interpretar_RetornaPrimeiroTelefoneNormalizado(string texto, string esperado)
    {
        Assert.Equal(esperado, InterpretadorCurriculo.Interpretar(texto).Telefone);
    }

    [Theory]
    [InlineData("CPF: 123.456.789-09")]
    [InlineData("CPF 52998224725")]
    [InlineData("Celular 41999998888")]
    [InlineData("CEP 80240-210")]
    [InlineData("CEP: 80.240-210")]
    [InlineData("CEP 80240210")]
    [InlineData("Nascimento: 14/06/1996")]
    [InlineData("Data: 14.06.1996")]
    [InlineData("Emitido em 2019-06-14")]
    [InlineData("Bacharelado | 2015 - 2019")]
    [InlineData("Técnico em Informática, 1998–2002")]
    [InlineData("Horizonte Sistemas | 02/2022 - Presente")]
    [InlineData("RG 12.345.678-9")]
    [InlineData("Processo 0001234-56.2020.8.16.0001")]
    [InlineData("Arquivos CNAB 240 e certificação AZ-204")]
    [InlineData("Telefone: 99999-8888")]
    public void Interpretar_OutrosNumeros_NaoSaoConfundidosComTelefone(string texto)
    {
        Assert.Null(InterpretadorCurriculo.Interpretar(texto).Telefone);
    }
}
