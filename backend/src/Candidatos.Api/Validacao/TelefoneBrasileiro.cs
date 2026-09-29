using System.Text.RegularExpressions;

namespace Candidatos.Api.Validacao;

public static partial class TelefoneBrasileiro
{
    public const string FormatoNormalizado = @"^\([1-9]{2}\) (9\d{4}|[2-5]\d{3})-\d{4}$";

    private const string Numero =
        @"(?:\+?55[\s.-]?)?(?:\((?<ddd>[1-9]{2})\)|(?<ddd>[1-9]{2}))[\s.-]?(?<prefixo>9[\s.]?\d{4}|[2-5]\d{3})[\s.-]?(?<final>\d{4})";

    [GeneratedRegex($"^{Numero}$")]
    private static partial Regex NumeroCompleto();

    public static string? Normalizar(string valor)
    {
        var numero = NumeroCompleto().Match(valor);
        return numero.Success ? Formatar(numero) : null;
    }

    private static string Formatar(Match numero)
    {
        var prefixo = numero.Groups["prefixo"].Value.Where(char.IsAsciiDigit).ToArray();
        return $"({numero.Groups["ddd"].Value}) {new string(prefixo)}-{numero.Groups["final"].Value}";
    }
}
