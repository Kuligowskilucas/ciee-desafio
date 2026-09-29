using Candidatos.Api.Data;
using Candidatos.Api.Dtos;
using Candidatos.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Candidatos.Api.Controllers;

[ApiController]
[Route("api/candidatos")]
public class CandidatosController(CandidatosDbContext db) : ControllerBase
{
    private const int ErroIndiceUnicoDuplicado = 2601;
    private const int ErroConstraintUnicaDuplicada = 2627;

    [HttpPost]
    public async Task<ActionResult<CandidatoDto>> Cadastrar(CriarCandidatoDto dto, CancellationToken cancellationToken)
    {
        var candidato = new Candidato
        {
            NomeCompleto = dto.NomeCompleto!,
            Email = dto.Email!,
            Telefone = dto.Telefone,
            AreaInteresse = dto.AreaInteresse,
            ResumoProfissional = dto.ResumoProfissional,
        };
        db.Candidatos.Add(candidato);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (EhViolacaoDeUnicidade(excecao))
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: "Já existe um candidato cadastrado com este e-mail.");
        }

        return CreatedAtAction(nameof(ObterPorId), new { id = candidato.Id }, CandidatoDto.De(candidato));
    }

    [HttpGet]
    public async Task<IReadOnlyList<CandidatoResumoDto>> Listar(CancellationToken cancellationToken) =>
        await db.Candidatos
            .AsNoTracking()
            .OrderByDescending(c => c.CriadoEm)
            .ThenByDescending(c => c.Id)
            .Select(c => new CandidatoResumoDto(c.Id, c.NomeCompleto, c.Email, c.AreaInteresse, c.CriadoEm))
            .ToListAsync(cancellationToken);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CandidatoDto>> ObterPorId(int id, CancellationToken cancellationToken)
    {
        var candidato = await db.Candidatos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return candidato is null ? NotFound() : CandidatoDto.De(candidato);
    }

    private static bool EhViolacaoDeUnicidade(DbUpdateException excecao) =>
        excecao.InnerException is SqlException { Number: ErroIndiceUnicoDuplicado or ErroConstraintUnicaDuplicada };
}
