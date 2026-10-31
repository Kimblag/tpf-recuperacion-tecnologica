using Microsoft.AspNetCore.Identity;
using TPF.RecuperacionTecnologica.Web.Models;

namespace TPF.RecuperacionTecnologica.Web.Services.Equipos;

public sealed class ResultadoLoteImagenes
{
    public IdentityResult Resultado { get; init; } = IdentityResult.Success;
    public IReadOnlyList<ImagenEquipo> Imagenes { get; init; } = [];
    public IReadOnlyList<string> Archivos { get; init; } = [];

    public static ResultadoLoteImagenes Fallido(IdentityError error) =>
        new() { Resultado = IdentityResult.Failed(error) };
}