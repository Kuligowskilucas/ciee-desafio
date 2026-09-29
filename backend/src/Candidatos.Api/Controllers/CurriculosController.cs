using Candidatos.Api.Curriculos;
using Candidatos.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Candidatos.Api.Controllers;

[ApiController]
[Route("api/curriculos")]
public class CurriculosController(ILogger<CurriculosController> logger) : ControllerBase
{
    private const string CampoDoArquivo = "arquivo";

    [HttpPost("extrair")]
    public async Task<ActionResult<DadosCurriculoDto>> Extrair()
    {
        try
        {
            var conteudo = await ArquivoEnviado.LerAsync(Request, CampoDoArquivo, HttpContext.RequestAborted);

            if (!LeitorPdf.TemAssinaturaPdf(conteudo))
            {
                return Problem(
                    statusCode: StatusCodes.Status415UnsupportedMediaType,
                    detail: "O arquivo enviado não é um PDF. Selecione um arquivo .pdf.");
            }

            return InterpretadorCurriculo.Interpretar(LeitorPdf.ExtrairTexto(conteudo));
        }
        catch (ArquivoRecusadoException excecao)
        {
            return Problem(statusCode: excecao.StatusCode, detail: excecao.Message);
        }
        catch (LeituraPdfException excecao)
        {
            logger.LogWarning(
                "PDF não lido: {Motivo} Exceção do PdfPig: {TipoDaExcecao}",
                excecao.Message,
                excecao.InnerException?.GetType().Name ?? "nenhuma");
            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: excecao.Message);
        }
    }
}
