namespace Candidatos.Api.Curriculos;

public class LeituraPdfException(string mensagem, Exception? causa = null) : Exception(mensagem, causa);
