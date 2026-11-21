using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace TPF.RecuperacionTecnologica.Web.Infraestructura;

public class BinderTextoNormalizado : IModelBinder
{
    private readonly IModelBinder _interno;
    private readonly bool _vacioANulo;

    public BinderTextoNormalizado(IModelBinder interno, bool vacioANulo)
    {
        _interno = interno;
        _vacioANulo = vacioANulo;
    }

    public async Task BindModelAsync(ModelBindingContext contexto)
    {
        await _interno.BindModelAsync(contexto);

        if (!contexto.Result.IsModelSet || contexto.Result.Model is not string texto)
        {
            return;
        }

        var normalizado = NormalizadorTexto.Normalizar(texto);

        contexto.Result = ModelBindingResult.Success(
            normalizado.Length == 0 && _vacioANulo ? null : normalizado);
    }
}

public class ProveedorBinderTextoNormalizado : IModelBinderProvider
{
    private static readonly string[] FragmentosOmitidos = ["password", "contrasena", "token", "idusuario"];

    public IModelBinder? GetBinder(ModelBinderProviderContext contexto)
    {
        var metadata = contexto.Metadata;

        if (metadata.ModelType != typeof(string) || DebeOmitirse(metadata))
        {
            return null;
        }

        var interno = new SimpleTypeModelBinder(typeof(string), contexto.Services.GetRequiredService<ILoggerFactory>());

        return new BinderTextoNormalizado(interno, metadata.ConvertEmptyStringToNull);
    }

    private static bool DebeOmitirse(ModelMetadata metadata)
    {
        if (metadata.DataTypeName == "Password")
        {
            return true;
        }

        var nombre = (metadata.PropertyName ?? metadata.Name ?? string.Empty).ToLowerInvariant();

        return FragmentosOmitidos.Any(nombre.Contains);
    }
}
