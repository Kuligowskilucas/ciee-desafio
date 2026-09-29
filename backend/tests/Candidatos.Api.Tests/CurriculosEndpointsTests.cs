using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Candidatos.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Candidatos.Api.Tests;

[Collection(nameof(ApiCollection))]
public class CurriculosEndpointsTests(ApiFixture fixture)
{
    private const string Rota = "/api/curriculos/extrair";
    private const int CincoMegabytes = 5 * 1024 * 1024;
    private const string MensagemSemArquivo = "Selecione um arquivo PDF para enviar.";
    private const string MensagemAcimaDoLimite = "O arquivo tem mais de 5 MB. Envie um PDF de até 5 MB.";
    private const string MensagemCorrompido =
        "Não foi possível ler o PDF; o arquivo pode estar corrompido. Preencha os dados manualmente.";

    private readonly HttpClient _client = fixture.Factory.CreateClient();

    public static TheoryData<byte[]> ArquivosQueNaoSaoPdf => new()
    {
        Encoding.UTF8.GetBytes("Maria da Silva - currículo em texto simples"),
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
        Encoding.ASCII.GetBytes(" %PDF-1.7"),
    };

    public static TheoryData<byte[]> PdfsCorrompidos => new()
    {
        Encoding.UTF8.GetBytes("%PDF-1.7\nisto não é um PDF de verdade"),
        PdfTruncado(),
    };

    public static TheoryData<byte[]> PdfsSemTexto => new()
    {
        PdfDeTeste.Exemplo("curriculo-digitalizado.pdf"),
        PdfDeTeste.Gerar([]),
    };

    private static byte[] PdfTruncado()
    {
        var pdf = PdfDeTeste.Gerar(["Maria da Silva", "maria@exemplo.com"]);
        return pdf[..(pdf.Length / 2)];
    }

    private static MultipartFormDataContent Formulario(byte[] conteudo, string campo = "arquivo")
    {
        var arquivo = new ByteArrayContent(conteudo);
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        return new MultipartFormDataContent { { arquivo, campo, "curriculo.pdf" } };
    }

    private static async Task<HttpResponseMessage> Enviar(HttpClient client, byte[] conteudo)
    {
        using var formulario = Formulario(conteudo);
        return await client.PostAsync(Rota, formulario);
    }

    private static async Task<ProblemDetails> LerProblema(HttpResponseMessage resposta, HttpStatusCode status, string titulo)
    {
        Assert.Equal(status, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problema);
        Assert.Equal(titulo, problema.Title);
        return problema;
    }

    [Fact]
    public async Task Extrair_CurriculoFicticioDeExemplos_RetornaNomeEmailETelefone()
    {
        var resposta = await Enviar(_client, PdfDeTeste.Exemplo("curriculo-ficticio.pdf"));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var dados = await resposta.Content.ReadFromJsonAsync<DadosCurriculoDto>();
        Assert.Equal(new DadosCurriculoDto("Mariana Alves Ferreira", "mariana.ferreira@example.com", "(41) 98765-4321"), dados);
    }

    [Fact]
    public async Task Extrair_PdfComCabecalhoECpf_RetornaDadosSemConfundirOCpf()
    {
        var pdf = PdfDeTeste.Gerar([
            "Curriculum Vitae",
            "JOAO PEDRO SOUZA",
            "CPF: 529.982.247-25",
            "Contato: Joao.Souza@Exemplo.com.br | +55 41 3333-4444",
        ]);

        var resposta = await Enviar(_client, pdf);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var dados = await resposta.Content.ReadFromJsonAsync<DadosCurriculoDto>();
        Assert.Equal(new DadosCurriculoDto("Joao Pedro Souza", "joao.souza@exemplo.com.br", "(41) 3333-4444"), dados);
    }

    [Fact]
    public async Task Extrair_PdfSemDadosIdentificaveis_Retorna200ComOsTresCamposNulos()
    {
        var resposta = await Enviar(_client, PdfDeTeste.Gerar(["experiencia com atendimento ao publico"]));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("nomeCompleto").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("email").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("telefone").ValueKind);
    }

    [Fact]
    public async Task Extrair_FormularioSemOCampoArquivo_Retorna400()
    {
        using var formulario = Formulario(PdfDeTeste.Gerar(["Maria da Silva"]), campo: "outro");

        var resposta = await _client.PostAsync(Rota, formulario);

        var problema = await LerProblema(resposta, HttpStatusCode.BadRequest, "Dados inválidos");
        Assert.Equal(MensagemSemArquivo, problema.Detail);
    }

    [Fact]
    public async Task Extrair_ArquivoVazio_Retorna400()
    {
        var resposta = await Enviar(_client, []);

        var problema = await LerProblema(resposta, HttpStatusCode.BadRequest, "Dados inválidos");
        Assert.Equal(MensagemSemArquivo, problema.Detail);
    }

    [Fact]
    public async Task Extrair_SemCorpo_Retorna400()
    {
        var resposta = await _client.PostAsync(Rota, content: null);

        var problema = await LerProblema(resposta, HttpStatusCode.BadRequest, "Dados inválidos");
        Assert.Equal(MensagemSemArquivo, problema.Detail);
    }

    [Fact]
    public async Task Extrair_MultipartMalformado_Retorna400()
    {
        using var conteudo = new StringContent("isto não é um multipart");
        conteudo.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data; boundary=limite");

        var resposta = await _client.PostAsync(Rota, conteudo);

        var problema = await LerProblema(resposta, HttpStatusCode.BadRequest, "Dados inválidos");
        Assert.Equal("Não foi possível ler o formulário enviado.", problema.Detail);
    }

    [Theory]
    [MemberData(nameof(ArquivosQueNaoSaoPdf))]
    public async Task Extrair_ArquivoQueNaoEhPdfMesmoComNomeEContentTypeDePdf_Retorna415(byte[] conteudo)
    {
        var resposta = await Enviar(_client, conteudo);

        var problema = await LerProblema(resposta, HttpStatusCode.UnsupportedMediaType, "Tipo de arquivo não suportado");
        Assert.Equal("O arquivo enviado não é um PDF. Selecione um arquivo .pdf.", problema.Detail);
    }

    [Fact]
    public async Task Extrair_ArquivoComExatamente5MB_NaoERecusadoPeloTamanho()
    {
        var resposta = await Enviar(_client, PdfDeTeste.ComAssinaturaPdf(CincoMegabytes));

        var problema = await LerProblema(resposta, HttpStatusCode.UnprocessableEntity, "Não foi possível ler o arquivo");
        Assert.Equal(MensagemCorrompido, problema.Detail);
    }

    [Fact]
    public async Task Extrair_ArquivoComUmByteAcimaDe5MB_Retorna413()
    {
        var resposta = await Enviar(_client, PdfDeTeste.ComAssinaturaPdf(CincoMegabytes + 1));

        var problema = await LerProblema(resposta, HttpStatusCode.RequestEntityTooLarge, "Arquivo muito grande");
        Assert.Equal(MensagemAcimaDoLimite, problema.Detail);
    }

    [Fact]
    public async Task Extrair_ArquivoDe20MBComKestrel_ClienteRecebe413ComAMensagem()
    {
        await using var factory = ApiFixture.CriarFactory("Development", fixture.ConnectionString);
        factory.UseKestrel(opcoes => opcoes.Listen(IPAddress.Loopback, 0));
        factory.StartServer();
        using var client = factory.CreateClient();

        var resposta = await Enviar(client, PdfDeTeste.ComAssinaturaPdf(20 * 1024 * 1024));

        var problema = await LerProblema(resposta, HttpStatusCode.RequestEntityTooLarge, "Arquivo muito grande");
        Assert.Equal(MensagemAcimaDoLimite, problema.Detail);
    }

    [Theory]
    [MemberData(nameof(PdfsCorrompidos))]
    public async Task Extrair_PdfCorrompido_Retorna422(byte[] conteudo)
    {
        var resposta = await Enviar(_client, conteudo);

        var problema = await LerProblema(resposta, HttpStatusCode.UnprocessableEntity, "Não foi possível ler o arquivo");
        Assert.Equal(MensagemCorrompido, problema.Detail);
    }

    [Fact]
    public async Task Extrair_PdfProtegidoPorSenha_Retorna422()
    {
        var resposta = await Enviar(_client, PdfDeTeste.Exemplo("curriculo-protegido.pdf"));

        var problema = await LerProblema(resposta, HttpStatusCode.UnprocessableEntity, "Não foi possível ler o arquivo");
        Assert.Equal(
            "O PDF está protegido por senha. Envie uma versão sem senha ou preencha os dados manualmente.",
            problema.Detail);
    }

    [Theory]
    [MemberData(nameof(PdfsSemTexto))]
    public async Task Extrair_PdfSemTextoSelecionavel_Retorna422(byte[] conteudo)
    {
        var resposta = await Enviar(_client, conteudo);

        var problema = await LerProblema(resposta, HttpStatusCode.UnprocessableEntity, "Não foi possível ler o arquivo");
        Assert.Equal(
            "O PDF não tem texto selecionável (pode ser um documento digitalizado). Preencha os dados manualmente.",
            problema.Detail);
    }
}
