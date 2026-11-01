using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace TPF.RecuperacionTecnologica.Web.Services.Equipos;

public class LimitePeticionImagenesFilter : IAuthorizationFilter
{
    private readonly ImagenesOptions _opciones;

    public LimitePeticionImagenesFilter(IOptions<ImagenesOptions> opciones)
    {
        _opciones = opciones.Value;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var caracteristica = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();

        if (caracteristica is { IsReadOnly: false })
        {
            caracteristica.MaxRequestBodySize = _opciones.LimiteDePeticionBytes;
        }
    }
}