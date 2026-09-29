using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Candidatos.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Candidatos.Api.Tests;

[Collection(nameof(ApiCollection))]
public class CandidatosEndpointsTests(ApiFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    private static string NovoEmail() => $"{Guid.NewGuid():N}@exemplo.com";

    private static object CandidatoValido(string? email = null) => new
    {
        nomeCompleto = "Maria da Silva",
        email = email ?? NovoEmail(),
        telefone = "(41) 99999-8888",
        areaInteresse = "Desenvolvimento",
        resumoProfissional = "Estudante de Análise e Desenvolvimento de Sistemas.",
    };

    private Task<HttpResponseMessage> Cadastrar(object corpo) => _client.PostAsJsonAsync("/api/candidatos", corpo);

    private static async Task<ValidationProblemDetails> LerErrosDeValidacao(HttpResponseMessage resposta)
    {
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var problema = await resposta.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problema);
        Assert.Equal("Dados inválidos", problema.Title);
        return problema;
    }

    [Fact]
    public async Task Cadastrar_ComDadosValidos_Retorna201ComCriadoEmDoBancoEmUtc()
    {
        var antes = DateTimeOffset.UtcNow.AddMinutes(-1);

        var resposta = await Cadastrar(CandidatoValido());

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("id").GetInt32();
        Assert.True(id > 0);
        Assert.Equal($"/api/candidatos/{id}", resposta.Headers.Location?.AbsolutePath);

        var criadoEmNoJson = json.RootElement.GetProperty("criadoEm").GetString();
        Assert.EndsWith("+00:00", criadoEmNoJson);
        var criadoEm = DateTimeOffset.Parse(criadoEmNoJson!);
        Assert.InRange(criadoEm, antes, DateTimeOffset.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task Cadastrar_NormalizaEspacosEmailEOpcionaisVazios()
    {
        var email = NovoEmail();

        var resposta = await Cadastrar(new
        {
            nomeCompleto = "  Maria da Silva  ",
            email = $"  {email.ToUpperInvariant()}  ",
            telefone = "",
            areaInteresse = "   ",
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var candidato = await resposta.Content.ReadFromJsonAsync<CandidatoDto>();
        Assert.NotNull(candidato);
        Assert.Equal("Maria da Silva", candidato.NomeCompleto);
        Assert.Equal(email, candidato.Email);
        Assert.Null(candidato.Telefone);
        Assert.Null(candidato.AreaInteresse);
        Assert.Null(candidato.ResumoProfissional);
    }

    [Fact]
    public async Task Cadastrar_SemNome_Retorna400ComMensagemEmPortugues()
    {
        var resposta = await Cadastrar(new { email = NovoEmail() });

        var problema = await LerErrosDeValidacao(resposta);
        Assert.Equal(["Informe o nome completo."], problema.Errors["NomeCompleto"]);
    }

    [Fact]
    public async Task Cadastrar_ComNomeSoDeEspacos_Retorna400()
    {
        var resposta = await Cadastrar(new { nomeCompleto = "   ", email = NovoEmail() });

        var problema = await LerErrosDeValidacao(resposta);
        Assert.Contains("NomeCompleto", problema.Errors.Keys);
    }

    [Theory]
    [InlineData("joao")]
    [InlineData("joao@exemplo")]
    [InlineData("joao@exemplo.c")]
    [InlineData("joao@.com")]
    [InlineData("joao@exemplo..com")]
    [InlineData("joao@exemplo.com.")]
    [InlineData("jo ao@exemplo.com")]
    [InlineData("a@b@exemplo.com")]
    public async Task Cadastrar_ComEmailInvalido_Retorna400(string email)
    {
        var resposta = await Cadastrar(CandidatoValido(email));

        var problema = await LerErrosDeValidacao(resposta);
        Assert.Equal(["Informe um e-mail válido, como nome@exemplo.com."], problema.Errors["Email"]);
    }

    [Theory]
    [InlineData("joao.silva+cv@empresa.com.br")]
    [InlineData("ana@sub.dominio.io")]
    [InlineData("joão@exemplo.com.br")]
    public async Task Cadastrar_ComEmailValido_Retorna201(string email)
    {
        var resposta = await Cadastrar(CandidatoValido($"{Guid.NewGuid():N}{email}"));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
    }

    [Fact]
    public async Task Cadastrar_ComNomeAcimaDoTamanhoMaximo_Retorna400()
    {
        var resposta = await Cadastrar(new { nomeCompleto = new string('a', 151), email = NovoEmail() });

        var problema = await LerErrosDeValidacao(resposta);
        Assert.Equal(["O nome completo deve ter no máximo 150 caracteres."], problema.Errors["NomeCompleto"]);
    }

    [Fact]
    public async Task Cadastrar_ComEmailJaCadastradoComOutrasMaiusculas_Retorna409()
    {
        var email = NovoEmail();
        var primeiro = await Cadastrar(CandidatoValido(email));
        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);

        var resposta = await Cadastrar(CandidatoValido(email.ToUpperInvariant()));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problema);
        Assert.Equal("Conflito com um registro existente", problema.Title);
        Assert.Equal("Já existe um candidato cadastrado com este e-mail.", problema.Detail);
    }

    [Fact]
    public async Task Listar_RetornaMaisRecentesPrimeiro()
    {
        var primeiro = await (await Cadastrar(CandidatoValido())).Content.ReadFromJsonAsync<CandidatoDto>();
        var segundo = await (await Cadastrar(CandidatoValido())).Content.ReadFromJsonAsync<CandidatoDto>();

        var lista = await _client.GetFromJsonAsync<List<CandidatoResumoDto>>("/api/candidatos");

        Assert.NotNull(lista);
        var ids = lista.Select(c => c.Id).ToList();
        Assert.Contains(primeiro!.Id, ids);
        Assert.Contains(segundo!.Id, ids);
        Assert.True(ids.IndexOf(segundo.Id) < ids.IndexOf(primeiro.Id));
    }

    [Fact]
    public async Task ObterPorId_Existente_Retorna200ComDados()
    {
        var criado = await (await Cadastrar(CandidatoValido())).Content.ReadFromJsonAsync<CandidatoDto>();

        var candidato = await _client.GetFromJsonAsync<CandidatoDto>($"/api/candidatos/{criado!.Id}");

        Assert.Equal(criado, candidato);
    }

    [Fact]
    public async Task ObterPorId_Inexistente_Retorna404ComProblemDetails()
    {
        var resposta = await _client.GetAsync($"/api/candidatos/{int.MaxValue}");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problema);
        Assert.Equal("Recurso não encontrado", problema.Title);
    }
}
