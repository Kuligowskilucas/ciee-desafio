using Candidatos.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Candidatos.Api.Data;

public class CandidatosDbContext(DbContextOptions<CandidatosDbContext> options) : DbContext(options)
{
    public DbSet<Candidato> Candidatos => Set<Candidato>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var candidato = modelBuilder.Entity<Candidato>();

        candidato.Property(c => c.NomeCompleto).HasMaxLength(Candidato.NomeCompletoTamanhoMaximo);
        candidato.Property(c => c.Email).HasMaxLength(Candidato.EmailTamanhoMaximo);
        candidato.Property(c => c.Telefone).HasMaxLength(Candidato.TelefoneTamanhoMaximo);
        candidato.Property(c => c.AreaInteresse).HasMaxLength(Candidato.AreaInteresseTamanhoMaximo);
        candidato.Property(c => c.ResumoProfissional).HasMaxLength(Candidato.ResumoProfissionalTamanhoMaximo);
        candidato.Property(c => c.CriadoEm).HasDefaultValueSql("SYSUTCDATETIME()");

        candidato.HasIndex(c => c.Email).IsUnique();
    }
}
