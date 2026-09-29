using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Candidatos.Api.Dtos;
using Candidatos.Api.Entities;
using Candidatos.Api.Validacao;

namespace Candidatos.Api.Curriculos;

public static partial class InterpretadorCurriculo
{
    private const int LinhasAnalisadasParaNome = 10;
    private const int PalavrasMinimasNoNome = 2;
    private const int PalavrasMaximasNoNome = 8;

    private static readonly HashSet<string> Conectivos = new(StringComparer.OrdinalIgnoreCase)
    {
        "da", "das", "de", "do", "dos", "e", "di", "du", "del", "van", "von",
    };

    private static readonly HashSet<string> PalavrasDeTituloOuCabecalho = new(StringComparer.OrdinalIgnoreCase)
    {
        "curriculo", "curriculum", "vitae", "cv", "resume", "dados", "pessoais", "perfil", "objetivo", "objetivos",
        "resumo", "profissional", "contato", "contatos", "informacoes", "experiencia", "experiencias", "formacao",
        "academica", "competencias", "habilidades", "idiomas",
        "desenvolvedor", "desenvolvedora", "analista", "engenheiro", "engenheira", "estagiario", "estagiaria",
        "assistente", "tecnico", "tecnica", "gerente", "programador", "programadora", "designer",
    };

    public static DadosCurriculoDto Interpretar(string texto) =>
        new(EncontrarNome(texto), EncontrarEmail(texto), TelefoneBrasileiro.EncontrarNoTexto(texto));

    [GeneratedRegex(@"[^\s()<>\[\]{},;:""'|]+@[^\s()<>\[\]{},;:""'|]+")]
    private static partial Regex TrechoComArroba();

    [GeneratedRegex(CriarCandidatoDto.FormatoEmail)]
    private static partial Regex FormatoEmail();

    private static string? EncontrarEmail(string texto) =>
        TrechoComArroba().Matches(texto)
            .Select(trecho => trecho.Value.TrimEnd('.'))
            .FirstOrDefault(FormatoEmail().IsMatch)
            ?.ToLowerInvariant();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacos();

    [GeneratedRegex(@"\s*[|•·–—]\s*|\s+-\s+")]
    private static partial Regex SeparadoresDeTrecho();

    [GeneratedRegex(@"^nome(\s+completo)?\s*:\s*", RegexOptions.IgnoreCase)]
    private static partial Regex RotuloDeNome();

    [GeneratedRegex(@"[^\p{L}]+")]
    private static partial Regex ForaDePalavra();

    [GeneratedRegex(@"^\p{Lu}(\p{L}*(['’-]\p{L}+)*|\.)$")]
    private static partial Regex PalavraDeNome();

    [GeneratedRegex(@"(?<=^|['’-])\p{L}")]
    private static partial Regex InicioDeParteDoNome();

    private static string? EncontrarNome(string texto)
    {
        var nome = texto.Split('\n')
            .Select(linha => Espacos().Replace(linha, " ").Trim())
            .Where(linha => linha.Length > 0)
            .Take(LinhasAnalisadasParaNome)
            .Select(PrimeiroTrechoForaDeCabecalho)
            .FirstOrDefault(TemFormatoDeNome);

        return nome is not null && !nome.Any(char.IsLower) ? ComIniciaisMaiusculas(nome) : nome;
    }

    private static string ComIniciaisMaiusculas(string nome) =>
        string.Join(' ', nome.Split(' ').Select(palavra => Conectivos.Contains(palavra)
            ? palavra.ToLowerInvariant()
            : InicioDeParteDoNome().Replace(palavra.ToLowerInvariant(), letra => letra.Value.ToUpperInvariant())));

    private static string? PrimeiroTrechoForaDeCabecalho(string linha) =>
        SeparadoresDeTrecho().Split(linha)
            .Select(trecho => RotuloDeNome().Replace(trecho, "").Trim())
            .Where(trecho => trecho.Length > 0)
            .FirstOrDefault(trecho => !EhTituloOuCabecalho(trecho));

    private static bool EhTituloOuCabecalho(string trecho) =>
        ForaDePalavra().Split(trecho).Any(palavra => PalavrasDeTituloOuCabecalho.Contains(SemAcentos(palavra)));

    private static bool TemFormatoDeNome(string? trecho)
    {
        if (trecho is null || trecho.Length > Candidato.NomeCompletoTamanhoMaximo)
        {
            return false;
        }

        var palavras = trecho.Split(' ');

        return palavras.Length is >= PalavrasMinimasNoNome and <= PalavrasMaximasNoNome
            && !Conectivos.Contains(palavras[0])
            && !Conectivos.Contains(palavras[^1])
            && palavras.All(palavra => Conectivos.Contains(palavra) || PalavraDeNome().IsMatch(palavra));
    }

    private static string SemAcentos(string palavra) =>
        string.Concat(palavra.Normalize(NormalizationForm.FormD)
            .Where(letra => CharUnicodeInfo.GetUnicodeCategory(letra) != UnicodeCategory.NonSpacingMark));
}
