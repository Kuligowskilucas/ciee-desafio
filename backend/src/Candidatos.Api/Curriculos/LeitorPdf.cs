using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Exceptions;

namespace Candidatos.Api.Curriculos;

public static class LeitorPdf
{
    public static bool TemAssinaturaPdf(ReadOnlySpan<byte> conteudo) => conteudo.StartsWith("%PDF-"u8);

    public static string ExtrairTexto(byte[] conteudo)
    {
        var texto = LerPaginas(conteudo);

        if (!texto.Any(char.IsLetter))
        {
            throw new LeituraPdfException(
                "O PDF não tem texto selecionável (pode ser um documento digitalizado). Preencha os dados manualmente.");
        }

        return texto;
    }

    private static string LerPaginas(byte[] conteudo)
    {
        try
        {
            using var documento = PdfDocument.Open(conteudo);
            var paginas = documento.GetPages().Select(pagina => ContentOrderTextExtractor.GetText(pagina));
            return string.Join('\n', paginas).ReplaceLineEndings("\n");
        }
        catch (PdfDocumentEncryptedException excecao)
        {
            throw new LeituraPdfException(
                "O PDF está protegido por senha. Envie uma versão sem senha ou preencha os dados manualmente.", excecao);
        }
        catch (Exception excecao)
        {
            throw new LeituraPdfException(
                "Não foi possível ler o PDF; o arquivo pode estar corrompido. Preencha os dados manualmente.", excecao);
        }
    }
}
