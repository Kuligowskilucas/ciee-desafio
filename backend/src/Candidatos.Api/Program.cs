using Candidatos.Api.Data;
using Microsoft.EntityFrameworkCore;

const string NomeConnectionString = "Candidatos";
const string MensagemConnectionStringAusente =
    "Connection string 'Candidatos' não configurada. Defina com: " +
    "dotnet user-secrets set \"ConnectionStrings:Candidatos\" \"<connection string>\" " +
    "--project backend/src/Candidatos.Api (ou pela variável de ambiente ConnectionStrings__Candidatos).";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CandidatosDbContext>((services, options) =>
    options.UseSqlServer(services.GetRequiredService<IConfiguration>().GetConnectionString(NomeConnectionString)
        ?? throw new InvalidOperationException(MensagemConnectionStringAusente)));
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Title = context.ProblemDetails.Status switch
    {
        StatusCodes.Status400BadRequest => "Dados inválidos",
        StatusCodes.Status404NotFound => "Recurso não encontrado",
        StatusCodes.Status409Conflict => "Conflito com um registro existente",
        StatusCodes.Status500InternalServerError => "Erro interno no servidor",
        _ => context.ProblemDetails.Title,
    });

var app = builder.Build();

if (app.Configuration.GetConnectionString(NomeConnectionString) is null)
{
    throw new InvalidOperationException(MensagemConnectionStringAusente);
}

app.UseExceptionHandler();
app.UseStatusCodePages();

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
