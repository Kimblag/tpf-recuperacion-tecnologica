using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Cuenta;

public class LoginViewModel
{
[Required(ErrorMessage = "El correo electrónico es obligatorio.")]
[EmailAddress(ErrorMessage = "Ingresá un correo electrónico válido.")]
public string CorreoElectronico { get; set; } = string.Empty;

[Required(ErrorMessage = "La contraseña es obligatoria.")]
[DataType(DataType.Password)]
public string Password { get; set; } = string.Empty;

public bool RecordarSesion { get; set; }

}