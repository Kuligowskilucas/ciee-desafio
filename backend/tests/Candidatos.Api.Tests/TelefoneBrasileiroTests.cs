using Candidatos.Api.Validacao;

namespace Candidatos.Api.Tests;

public class TelefoneBrasileiroTests
{
    [Theory]
    [InlineData("(41) 99999-8888", "(41) 99999-8888")]
    [InlineData("(41)99999-8888", "(41) 99999-8888")]
    [InlineData("41 99999-8888", "(41) 99999-8888")]
    [InlineData("41 99999 8888", "(41) 99999-8888")]
    [InlineData("41.99999.8888", "(41) 99999-8888")]
    [InlineData("41-99999-8888", "(41) 99999-8888")]
    [InlineData("41999998888", "(41) 99999-8888")]
    [InlineData("(41) 9 9999-8888", "(41) 99999-8888")]
    [InlineData("(41) 9.9999-8888", "(41) 99999-8888")]
    [InlineData("+55 41 99999-8888", "(41) 99999-8888")]
    [InlineData("+55 (41) 99999-8888", "(41) 99999-8888")]
    [InlineData("+5541999998888", "(41) 99999-8888")]
    [InlineData("55 41 99999-8888", "(41) 99999-8888")]
    [InlineData("(41) 3333-4444", "(41) 3333-4444")]
    [InlineData("41 5333 4444", "(41) 5333-4444")]
    [InlineData("4123334444", "(41) 2333-4444")]
    public void Normalizar_FormatoBrasileiroComDdd_RetornaNoFormatoPadrao(string valor, string esperado)
    {
        Assert.Equal(esperado, TelefoneBrasileiro.Normalizar(valor));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("99999-8888")]
    [InlineData("3333-4444")]
    [InlineData("(41) 1234-5678")]
    [InlineData("(41) 6333-4444")]
    [InlineData("(41) 9999-8888")]
    [InlineData("(01) 99999-8888")]
    [InlineData("(40) 99999-8888")]
    [InlineData("(041) 99999-8888")]
    [InlineData("41 99999-88889")]
    [InlineData("+1 555 123 4567")]
    [InlineData("(41) 99999-8888 ramal 12")]
    public void Normalizar_ForaDoFormato_RetornaNull(string valor)
    {
        Assert.Null(TelefoneBrasileiro.Normalizar(valor));
    }
}
