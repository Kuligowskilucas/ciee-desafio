using Candidatos.Api.Entities;

namespace Candidatos.Api.Dtos;

public record CandidatoDto(
    int Id,
    string NomeCompleto,
    string Email,
    string? Telefone,
    string? AreaInteresse,
    string? ResumoProfissional,
    DateTimeOffset CriadoEm)
{
    public static CandidatoDto De(Candidato candidato) => new(
        candidato.Id,
        candidato.NomeCompleto,
        candidato.Email,
        candidato.Telefone,
        candidato.AreaInteresse,
        candidato.ResumoProfissional,
        candidato.CriadoEm);
}
