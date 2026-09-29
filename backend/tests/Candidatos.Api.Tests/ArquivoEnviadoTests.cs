using Candidatos.Api.Curriculos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Candidatos.Api.Tests;

public class ArquivoEnviadoTests
{
    [Fact]
    public async Task LerAsync_FormularioJaLidoSemOsLimites_LancaInvalidOperationException()
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.ContentType = "multipart/form-data; boundary=limite";
        contexto.Features.Set<IFormFeature>(new FormFeature(new FormCollection(null)));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ArquivoEnviado.LerAsync(contexto.Request, "arquivo", CancellationToken.None));
    }
}
