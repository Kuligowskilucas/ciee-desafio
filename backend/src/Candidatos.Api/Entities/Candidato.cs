namespace Candidatos.Api.Entities;

public class Candidato
{
    public const int NomeCompletoTamanhoMaximo = 150;
    public const int EmailTamanhoMaximo = 254;
    public const int TelefoneTamanhoMaximo = 20;
    public const int AreaInteresseTamanhoMaximo = 100;
    public const int ResumoProfissionalTamanhoMaximo = 2000;

    public int Id { get; set; }
    public required string NomeCompleto { get; set; }
    public required string Email { get; set; }
    public string? Telefone { get; set; }
    public string? AreaInteresse { get; set; }
    public string? ResumoProfissional { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
}
