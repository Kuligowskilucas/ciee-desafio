namespace Candidatos.Api.Curriculos;

public class ArquivoRecusadoException(int statusCode, string mensagem) : Exception(mensagem)
{
    public int StatusCode { get; } = statusCode;
}
