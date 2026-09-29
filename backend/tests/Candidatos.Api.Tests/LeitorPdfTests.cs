using Candidatos.Api.Curriculos;

namespace Candidatos.Api.Tests;

public class LeitorPdfTests
{
    [Fact]
    public void ExtrairTexto_PreservaQuebrasDeLinhaESeparaPaginas()
    {
        var pdf = PdfDeTeste.Gerar(
            ["MARIA DA SILVA", "maria@exemplo.com"],
            ["Experiencia", "Empresa Exemplo"]);

        var texto = LeitorPdf.ExtrairTexto(pdf);

        Assert.Equal("MARIA DA SILVA\nmaria@exemplo.com\nExperiencia\nEmpresa Exemplo", texto);
    }
}
