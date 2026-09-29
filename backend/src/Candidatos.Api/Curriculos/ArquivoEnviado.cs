using Microsoft.AspNetCore.Http.Features;

namespace Candidatos.Api.Curriculos;

public static class ArquivoEnviado
{
    public const int TamanhoMaximoEmMegabytes = 5;
    public const int TamanhoMaximoEmBytes = TamanhoMaximoEmMegabytes * 1024 * 1024;

    private static readonly FormOptions LimitesDoFormulario = new()
    {
        MultipartBodyLengthLimit = TamanhoMaximoEmBytes,
        MemoryBufferThreshold = TamanhoMaximoEmBytes,
    };

    public static async Task<byte[]> LerAsync(HttpRequest requisicao, string campo, CancellationToken cancellationToken)
    {
        var arquivo = await LerDoFormularioAsync(requisicao, campo, cancellationToken);

        if (arquivo is null || arquivo.Length == 0)
        {
            throw new ArquivoRecusadoException(StatusCodes.Status400BadRequest, "Selecione um arquivo PDF para enviar.");
        }

        var conteudo = new byte[arquivo.Length];
        await using var leitura = arquivo.OpenReadStream();
        await leitura.ReadExactlyAsync(conteudo, cancellationToken);
        return conteudo;
    }

    private static async Task<IFormFile?> LerDoFormularioAsync(HttpRequest requisicao, string campo, CancellationToken cancellationToken)
    {
        if (!requisicao.HasFormContentType)
        {
            return null;
        }

        if (requisicao.HttpContext.Features.Get<IFormFeature>()?.Form is not null)
        {
            throw new InvalidOperationException(
                "O formulário já foi lido sem os limites de tamanho e de memória (por exemplo, pelo model binding " +
                "de um parâmetro da action). Leia o arquivo só pelo ArquivoEnviado.");
        }

        try
        {
            var formulario = await requisicao.ReadFormAsync(LimitesDoFormulario, cancellationToken);
            return formulario.Files.GetFile(campo);
        }
        catch (BadHttpRequestException excecao) when (excecao.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            throw AcimaDoTamanhoMaximo();
        }
        catch (InvalidDataException) when (requisicao.ContentLength > TamanhoMaximoEmBytes)
        {
            throw AcimaDoTamanhoMaximo();
        }
        catch (Exception excecao) when (excecao is InvalidDataException or IOException)
        {
            throw new ArquivoRecusadoException(StatusCodes.Status400BadRequest, "Não foi possível ler o formulário enviado.");
        }
    }

    private static ArquivoRecusadoException AcimaDoTamanhoMaximo() => new(
        StatusCodes.Status413PayloadTooLarge,
        $"O arquivo tem mais de {TamanhoMaximoEmMegabytes} MB. Envie um PDF de até {TamanhoMaximoEmMegabytes} MB.");
}
