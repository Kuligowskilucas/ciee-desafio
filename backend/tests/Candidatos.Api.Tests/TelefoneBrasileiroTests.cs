using Candidatos.Api.Validacao;

namespace Candidatos.Api.Tests;

public class TelefoneBrasileiroTests
{
    [Theory]
    [MemberData(nameof(CasosDeValidacao.TelefonesValidos), MemberType = typeof(CasosDeValidacao))]
    public void Normalizar_FormatoBrasileiroComDdd_RetornaNoFormatoPadrao(string valor, string esperado)
    {
        Assert.Equal(esperado, TelefoneBrasileiro.Normalizar(valor));
    }

    [Theory]
    [MemberData(nameof(CasosDeValidacao.TelefonesInvalidos), MemberType = typeof(CasosDeValidacao))]
    public void Normalizar_ForaDoFormato_RetornaNull(string valor)
    {
        Assert.Null(TelefoneBrasileiro.Normalizar(valor));
    }
}
