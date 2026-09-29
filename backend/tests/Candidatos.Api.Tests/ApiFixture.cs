using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Testcontainers.MsSql;

namespace Candidatos.Api.Tests;

public class ApiFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _sqlServer.StartAsync();

        var connectionString = new SqlConnectionStringBuilder(_sqlServer.GetConnectionString())
        {
            InitialCatalog = "CandidatosTestes",
        }.ConnectionString;

        Factory = CriarFactory("Development", connectionString);
        Factory.CreateClient().Dispose();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _sqlServer.DisposeAsync();
    }

    public static WebApplicationFactory<Program> CriarFactory(string ambiente, string connectionString) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(ambiente);
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureAppConfiguration((_, configuracao) =>
                configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Candidatos"] = connectionString,
                }));
        });
}

[CollectionDefinition(nameof(ApiCollection))]
public class ApiCollection : ICollectionFixture<ApiFixture>;
