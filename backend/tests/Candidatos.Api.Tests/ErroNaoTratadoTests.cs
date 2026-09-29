using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Candidatos.Api.Tests;

public class ErroNaoTratadoTests
{
    private const string ServidorInexistente =
        "Server=127.0.0.1,1;Database=Candidatos;User Id=sa;Password=senha-falsa;Connect Timeout=1;TrustServerCertificate=True";

    [Fact]
    public async Task ErroNaoTratado_Retorna500ComProblemDetailsSemStackTrace()
    {
        await using var factory = ApiFixture.CriarFactory("Production", ServidorInexistente);
        using var client = factory.CreateClient();

        var resposta = await client.GetAsync("/api/candidatos");

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SqlException", corpo);
        Assert.DoesNotContain(" at ", corpo);
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problema);
        Assert.Equal("Erro interno no servidor", problema.Title);
        Assert.Null(problema.Detail);
    }
}
