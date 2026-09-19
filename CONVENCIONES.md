# Convenciones de desarrollo

Este documento define las convenciones de desarrollo a utilizar dentro del proyecto.

El objetivo es mantener una implementación consistente y facilitar la lectura, revisión y mantenimiento del código.

## 1. Estructura general

El proyecto utiliza ASP.NET Core MVC en un único proyecto.

La estructura principal es:

```text
src/TPF.RecuperacionTecnologica.Web/
├── Controllers/
├── Data/
│   ├── Configurations/
│   └── ApplicationDbContext.cs
├── Domain/
│   ├── Entities/
│   └── Enums/
├── Identity/
├── Services/
├── ViewModels/
├── Views/
├── Migrations/
└── wwwroot/
```

Cada elemento debe ubicarse en la carpeta correspondiente a su responsabilidad.

## 2. Clases

Las clases utilizan `PascalCase`.

Ejemplos:

```csharp
public class UsuarioService
{
}

public class RegistrarDonacionViewModel
{
}

public class SolicitudesController
{
}
```

No utilizar nombres abreviados excepto abreviaturas ampliamente establecidas por el framework o el dominio.

## 3. Entidades

Las entidades del dominio utilizan nombres en singular y `PascalCase`.

Ejemplos:

```text
Usuario
Personal
Equipo
Donacion
Diagnostico
Solicitud
Asignacion
LiberacionAsignacion
ActaEntrega
ParametroInstitucional
Auditoria
```

Las entidades se ubican en:

```text
Domain/Entities/
```

Las entidades representan conceptos del dominio y no deben utilizarse directamente como modelos específicos de formularios o pantallas.

## 4. Propiedades

Las propiedades utilizan `PascalCase`.

Ejemplos:

```csharp
public int NroEquipo { get; set; }

public string Marca { get; set; }

public DateTime FechaRegistro { get; set; }

public EstadoEquipo EstadoEquipo { get; set; }
```

Los nombres deben ser descriptivos y evitar abreviaturas innecesarias.

Para valores booleanos utilizar nombres que expresen claramente su significado:

```csharp
public bool EsVigente { get; set; }
```

## 5. Métodos

Los métodos utilizan `PascalCase` y deben tener nombres que expresen claramente la operación realizada.

Ejemplos:

```csharp
Listar()
ObtenerPorId()
Registrar()
Modificar()
Cancelar()
Asignar()
Liberar()
RegistrarEntrega()
```

Los métodos deben realizar una responsabilidad concreta y evitar concentrar múltiples procesos independientes.

## 6. Services

Los Services utilizan el nombre del concepto o entidad seguido de `Service`.

Ejemplos:

```text
UsuarioService
PersonalService
EquipoService
DonacionService
DiagnosticoService
SolicitudService
AsignacionService
AuditoriaService
ReporteService
```

Se ubican en:

```text
Services/
```

Los Services contienen la lógica de negocio que no corresponde directamente al Controller.

Por ejemplo:

* validación de reglas de negocio;
* cambios de estado;
* coordinación de varias operaciones;
* transacciones;
* generación de registros de auditoría.

Los Controllers no deben concentrar la lógica de negocio.

## 7. ViewModels

Los ViewModels utilizan el nombre de la pantalla u operación seguido de `ViewModel`.

Ejemplos:

```text
RegistrarDonacionViewModel
EditarUsuarioViewModel
SolicitudDetalleViewModel
RegistrarDiagnosticoViewModel
ConfigurarParametrosViewModel
AuditoriaListadoViewModel
```

Se ubican en:

```text
ViewModels/
```

Los ViewModels deben contener únicamente los datos necesarios para una determinada vista u operación.

No se debe utilizar una entidad de dominio como modelo de un formulario cuando el formulario requiere un conjunto diferente de campos.

## 8. Controllers

Los Controllers utilizan el nombre del recurso en plural seguido de `Controller`.

Ejemplos:

```text
UsuariosController
PersonalController
EquiposController
DonacionesController
DiagnosticosController
SolicitudesController
AsignacionesController
AuditoriaController
ReportesController
```

Se ubican en:

```text
Controllers/
```

Los Controllers deben:

1. recibir la solicitud HTTP;
2. validar el modelo de entrada;
3. invocar el Service correspondiente;
4. seleccionar la respuesta o vista;
5. devolver el resultado HTTP.

La lógica de negocio debe permanecer en los Services.

## 9. Métodos asíncronos

Utilizar métodos `async` cuando la operación implique I/O, especialmente:

* consultas a SQL Server;
* inserciones o actualizaciones mediante EF Core;
* operaciones de Identity;
* acceso a archivos;
* llamadas a servicios externos.

Ejemplos:

```csharp
public async Task<IActionResult> Index()
{
    var usuarios = await _usuarioService.ListarAsync();

    return View(usuarios);
}
```

```csharp
public async Task<List<Usuario>> ListarAsync()
{
    return await _context.Usuarios
        .ToListAsync();
}
```

Los métodos asíncronos deben utilizar el sufijo `Async`.

Ejemplos:

```text
ListarAsync()
ObtenerPorIdAsync()
RegistrarAsync()
ModificarAsync()
CancelarAsync()
```

No utilizar `async` únicamente por convención cuando el método no realiza operaciones asíncronas.

No utilizar `.Result` ni `.Wait()` para bloquear operaciones asíncronas.

## 10. Configuraciones de Entity Framework Core

Las configuraciones de EF Core se implementan mediante `IEntityTypeConfiguration<T>`.

Cada entidad debe tener su propia configuración cuando necesite reglas de persistencia.

Las configuraciones se ubican en:

```text
Data/Configurations/
```

Ejemplos:

```text
UsuarioConfiguration.cs
EquipoConfiguration.cs
DonacionConfiguration.cs
SolicitudConfiguration.cs
AsignacionConfiguration.cs
```

Ejemplo:

```csharp
public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        // configuración de la entidad
    }
}
```

El `ApplicationDbContext` debe aplicar las configuraciones mediante:

```csharp
builder.ApplyConfigurationsFromAssembly(
    typeof(ApplicationDbContext).Assembly);
```

Las reglas específicas de persistencia deben mantenerse en las configuraciones de EF Core y no mezclarse innecesariamente con la lógica de negocio.

## 11. Enums

Los enums utilizan nombres en singular y `PascalCase`.

Ejemplos:

```text
EstadoUsuario
RolPersonal
EstadoEquipo
EstadoSolicitud
EstadoAsignacion
TipoDiagnostico
DictamenTecnico
TipoDocumento
TipoSolicitante
OrigenSolicitud
```

Los valores también utilizan `PascalCase`.

Ejemplo:

```csharp
public enum EstadoEquipo
{
    PendienteRecepcion,
    Disponible,
    EnReparacion,
    Asignado,
    Entregado,
    Baja,
    Cancelado
}
```

Los enums representan conjuntos cerrados de valores definidos por el dominio.

## 12. Variables y parámetros

Las variables locales y parámetros utilizan `camelCase`.

Ejemplos:

```csharp
var usuario = ...
var nroEquipo = ...
var fechaSolicitud = ...
```

Los campos privados utilizan `camelCase` con `_`.

Ejemplo:

```csharp
private readonly ApplicationDbContext _context;
```

Las constantes utilizan `PascalCase`.

```csharp
private const int DiasMaximos = 30;
```

## 13. Inyección de dependencias

Las dependencias deben recibirse mediante el constructor.

Ejemplo:

```csharp
public class DonacionesController : Controller
{
    private readonly DonacionService _donacionService;

    public DonacionesController(DonacionService donacionService)
    {
        _donacionService = donacionService;
    }
}
```

Evitar crear manualmente Services, DbContexts u otras dependencias dentro de Controllers.

## 14. Controllers y Services

La separación de responsabilidades seguirá este flujo:

```text
View
  ↓
ViewModel
  ↓
Controller
  ↓
Service
  ↓
ApplicationDbContext
  ↓
EF Core
  ↓
SQL Server
```

El Controller coordina la petición.

El Service implementa la operación de negocio.

El `ApplicationDbContext` gestiona la persistencia.

## 15. Validaciones

Las validaciones de entrada correspondientes a la interfaz pueden utilizar Data Annotations en ViewModels.

Las reglas de negocio deben validarse en los Services.

Ejemplo:

```text
ViewModel
→ campo obligatorio
→ formato básico

Service
→ equipo disponible
→ solicitud duplicada
→ transición de estado válida
→ permisos de operación
```

No confiar únicamente en las validaciones del frontend.

Toda regla crítica debe validarse del lado del servidor.

## 16. Mensajes de commit

Los commits utilizan una categoría en español seguida de `:` y una descripción breve.

Categorías permitidas:

```text
configuración:
funcionalidad:
corrección:
refactorización:
pruebas:
documentación:
```

Ejemplos:

```text
configuración: configurar EF Core
funcionalidad: registrar donación
corrección: validar solicitud duplicada
refactorización: separar lógica de asignación
pruebas: validar transición de estado
documentación: actualizar README
```

La descripción debe ser concreta y expresar qué cambio se realizó.

## 17. Branches

Cada tarea debe desarrollarse en una branch independiente.

Convención:

```text
feature/<numero>-<descripcion>
fix/<numero>-<descripcion>
chore/<numero>-<descripcion>
```

Ejemplos:

```text
feature/4.2-registrar-donacion
fix/6.4-validar-solicitud
chore/0.3-configurar-ef-core
```

Usar:

* `feature/` para funcionalidades nuevas;
* `fix/` para correcciones;
* `chore/` para configuración, mantenimiento o tareas técnicas.

No realizar desarrollo directamente sobre `main`.

## 18. Pull Requests

Los cambios deben integrarse mediante Pull Requests.

Flujo:

```text
Issue
  ↓
Branch
  ↓
Desarrollo
  ↓
Commit
  ↓
Push
  ↓
Pull Request
  ↓
Review
  ↓
Merge
  ↓
main
```

El Pull Request debe estar relacionado con la Issue correspondiente.

Cuando corresponda, utilizar referencias como:

```text
Closes #123
```

para vincular el Pull Request con la Issue.

## 19. Reglas generales

* Mantener una responsabilidad clara por clase.
* Evitar duplicación de lógica.
* No colocar lógica de negocio compleja en Controllers.
* No exponer entidades directamente en formularios cuando corresponda utilizar un ViewModel.
* No almacenar secretos en el repositorio.
* Mantener las configuraciones de EF Core en `Data/Configurations`.
* Utilizar `async/await` para operaciones de I/O.
* Validar reglas críticas en el servidor.
* Mantener los nombres consistentes con estas convenciones.
* Ante una excepción justificada a estas reglas, priorizar la claridad y documentar el motivo cuando sea necesario.
