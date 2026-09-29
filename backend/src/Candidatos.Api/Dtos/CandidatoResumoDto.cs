namespace Candidatos.Api.Dtos;

public record CandidatoResumoDto(
    int Id,
    string NomeCompleto,
    string Email,
    string? AreaInteresse,
    DateTimeOffset CriadoEm);
