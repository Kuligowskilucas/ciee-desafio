using System.ComponentModel.DataAnnotations;
using Candidatos.Api.Entities;
using Candidatos.Api.Validacao;

namespace Candidatos.Api.Dtos;

public class CriarCandidatoDto
{
    public const string FormatoEmail = @"^[^\s@]+@([^\s@.]+\.)+[^\s@.]{2,}$";

    [Required(ErrorMessage = "Informe o nome completo.")]
    [StringLength(Candidato.NomeCompletoTamanhoMaximo, ErrorMessage = "O nome completo deve ter no máximo {1} caracteres.")]
    public string? NomeCompleto { get; init => field = Normalizar(value); }

    [Required(ErrorMessage = "Informe o e-mail.")]
    [StringLength(Candidato.EmailTamanhoMaximo, ErrorMessage = "O e-mail deve ter no máximo {1} caracteres.")]
    [RegularExpression(FormatoEmail, ErrorMessage = "Informe um e-mail válido, como nome@exemplo.com.")]
    public string? Email { get; init => field = Normalizar(value)?.ToLowerInvariant(); }

    [RegularExpression(TelefoneBrasileiro.FormatoNormalizado, ErrorMessage = "Informe um telefone com DDD, como (41) 99999-8888.")]
    public string? Telefone { get; init => field = NormalizarTelefone(value); }

    [StringLength(Candidato.AreaInteresseTamanhoMaximo, ErrorMessage = "A área de interesse deve ter no máximo {1} caracteres.")]
    public string? AreaInteresse { get; init => field = Normalizar(value); }

    [StringLength(Candidato.ResumoProfissionalTamanhoMaximo, ErrorMessage = "O resumo profissional deve ter no máximo {1} caracteres.")]
    public string? ResumoProfissional { get; init => field = Normalizar(value); }

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string? NormalizarTelefone(string? valor) =>
        Normalizar(valor) is { } telefone ? TelefoneBrasileiro.Normalizar(telefone) ?? telefone : null;
}
