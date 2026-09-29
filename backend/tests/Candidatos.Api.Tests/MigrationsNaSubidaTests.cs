using System.Net;
using Microsoft.Data.SqlClient;

namespace Candidatos.Api.Tests;

[Collection(nameof(ApiCollection))]
public class MigrationsNaSubidaTests(ApiFixture fixture)
{
    private static readonly Dictionary<string, string?> FlagLigada = new()
    {
        ["Migrations:AplicarAoIniciar"] = "true",
    };

    [Fact]
    public async Task ForaDeDevelopment_ComAFlagLigada_CriaOBancoAoIniciar()
    {
        var connectionString = ConnectionStringDeBancoNovo();
        await using var factory = ApiFixture.CriarFactory("Production", connectionString, FlagLigada);
        using var client = factory.CreateClient();

        var resposta = await client.GetAsync("/api/candidatos");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("[]", await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ForaDeDevelopment_SemAFlag_NaoCriaOBanco()
    {
        var connectionString = ConnectionStringDeBancoNovo();
        await using var factory = ApiFixture.CriarFactory("Production", connectionString);
        using var client = factory.CreateClient();

        Assert.False(await BancoExiste(connectionString));
    }

    private string ConnectionStringDeBancoNovo() =>
        new SqlConnectionStringBuilder(fixture.ConnectionString)
        {
            InitialCatalog = $"MigrationsNaSubida_{Guid.NewGuid():N}",
        }.ConnectionString;

    private static async Task<bool> BancoExiste(string connectionString)
    {
        var construtor = new SqlConnectionStringBuilder(connectionString);
        var nomeDoBanco = construtor.InitialCatalog;
        construtor.InitialCatalog = "master";

        await using var conexao = new SqlConnection(construtor.ConnectionString);
        await conexao.OpenAsync();
        await using var comando = new SqlCommand("SELECT DB_ID(@nome)", conexao);
        comando.Parameters.AddWithValue("@nome", nomeDoBanco);
        return await comando.ExecuteScalarAsync() is not DBNull;
    }
}
