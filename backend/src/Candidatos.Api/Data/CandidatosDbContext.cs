using Candidatos.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Candidatos.Api.Data;

public class CandidatosDbContext(DbContextOptions<CandidatosDbContext> options) : DbContext(options)
{
    public DbSet<Candidato> Candidatos => Set<Candidato>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var candidato = modelBuilder.Entity<Candidato>();

        candidato.Property(c => c.NomeCompleto).HasMaxLength(150);
        candidato.Property(c => c.Email).HasMaxLength(254);
        candidato.Property(c => c.Telefone).HasMaxLength(20);
        candidato.Property(c => c.AreaInteresse).HasMaxLength(100);
        candidato.Property(c => c.ResumoProfissional).HasMaxLength(2000);
        candidato.Property(c => c.CriadoEm).HasDefaultValueSql("SYSUTCDATETIME()");

        candidato.HasIndex(c => c.Email).IsUnique();
    }
}
