namespace Candidatos.Api.Entities;

public class Candidato
{
    public int Id { get; set; }
    public required string NomeCompleto { get; set; }
    public required string Email { get; set; }
    public string? Telefone { get; set; }
    public string? AreaInteresse { get; set; }
    public string? ResumoProfissional { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
}
