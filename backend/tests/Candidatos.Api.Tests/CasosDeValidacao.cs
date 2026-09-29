using System.Text.Json;

namespace Candidatos.Api.Tests;

public static class CasosDeValidacao
{
    private static readonly Casos Todos = Carregar();

    public static TheoryData<string> EmailsValidos => new(Todos.Email.Validos);

    public static TheoryData<string> EmailsInvalidos => new(Todos.Email.Invalidos);

    public static TheoryData<string> TelefonesInvalidos => new(Todos.Telefone.Invalidos);

    public static TheoryData<string, string> TelefonesValidos
    {
        get
        {
            var dados = new TheoryData<string, string>();
            foreach (var telefone in Todos.Telefone.Validos)
            {
                dados.Add(telefone.Valor, telefone.Normalizado);
            }

            return dados;
        }
    }

    private static Casos Carregar()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "casos-de-validacao.json"));
        return JsonSerializer.Deserialize<Casos>(json, JsonSerializerOptions.Web)!;
    }

    private record Casos(CasosDeEmail Email, CasosDeTelefone Telefone);

    private record CasosDeEmail(string[] Validos, string[] Invalidos);

    private record CasosDeTelefone(TelefoneValido[] Validos, string[] Invalidos);

    private record TelefoneValido(string Valor, string Normalizado);
}
