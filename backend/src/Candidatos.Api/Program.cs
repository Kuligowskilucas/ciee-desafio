using Candidatos.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Candidatos")
    ?? throw new InvalidOperationException(
        "Connection string 'Candidatos' não configurada. Defina com: " +
        "dotnet user-secrets set \"ConnectionStrings:Candidatos\" \"<connection string>\" " +
        "--project backend/src/Candidatos.Api (ou pela variável de ambiente ConnectionStrings__Candidatos).");

builder.Services.AddDbContext<CandidatosDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<CandidatosDbContext>().Database.Migrate();

    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
