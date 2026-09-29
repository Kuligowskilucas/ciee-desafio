using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Candidatos.Api.Tests;

public static class PdfDeTeste
{
    private const double MargemEsquerda = 50;
    private const double AlturaDaPrimeiraLinha = 780;
    private const double EspacoEntreLinhas = 20;

    public static byte[] Gerar(params string[][] linhasPorPagina)
    {
        var builder = new PdfDocumentBuilder();
        var fonte = builder.AddStandard14Font(Standard14Font.Helvetica);

        foreach (var linhas in linhasPorPagina)
        {
            var pagina = builder.AddPage(PageSize.A4);
            for (var indice = 0; indice < linhas.Length; indice++)
            {
                var posicao = new PdfPoint(MargemEsquerda, AlturaDaPrimeiraLinha - indice * EspacoEntreLinhas);
                pagina.AddText(linhas[indice], 12, posicao, fonte);
            }
        }

        return builder.Build();
    }

    public static byte[] Exemplo(string nomeDoArquivo) =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "exemplos", nomeDoArquivo));

    public static byte[] ComAssinaturaPdf(int tamanhoEmBytes)
    {
        var conteudo = new byte[tamanhoEmBytes];
        "%PDF-1.7\n"u8.CopyTo(conteudo);
        return conteudo;
    }
}
