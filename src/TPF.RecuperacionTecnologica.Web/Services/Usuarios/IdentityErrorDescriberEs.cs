namespace TPF.RecuperacionTecnologica.Web.Services.Usuarios;

using Microsoft.AspNetCore.Identity;

/// <summary>
/// Clase que describe los errores de Identity en español. 
/// Se puede usar para personalizar los mensajes de error 
/// que se muestran al usuario cuando hay problemas con la 
/// autenticación o autorización.   
/// </summary>
public class IdentityErrorDescriberEs : IdentityErrorDescriber
{
    public override IdentityError DefaultError() =>
        new() { Code = nameof(DefaultError), Description = "Ocurrió un error inesperado. Intente nuevamente." };

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"La contraseña debe tener al menos {length} caracteres." };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = "La contraseña debe incluir al menos una letra mayúscula." };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "La contraseña debe incluir al menos una letra minúscula." };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "La contraseña debe incluir al menos un número." };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "La contraseña debe incluir al menos un símbolo." };

    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = "No se pudo completar el registro con los datos ingresados. Si ya tiene una cuenta, inicie sesión o recupere su contraseña." };

    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = "No se pudo completar el registro con los datos ingresados. Si ya tiene una cuenta, inicie sesión o recupere su contraseña." };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = "El correo electrónico no es válido." };

    public override IdentityError InvalidUserName(string? userName) =>
        new() { Code = nameof(InvalidUserName), Description = "El correo electrónico contiene caracteres no permitidos." };
}
