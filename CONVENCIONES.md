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
├── Models/
│   ├── Enums/
│   └── <Entidad>.cs
├── Services/
│   └── <Funcionalidad>/
│       ├── I<Nombre>Service.cs
│       └── <Nombre>Service.cs
├── ViewModels/
│   └── <Funcionalidad>/
│       └── <Nombre>ViewModel.cs
├── Views/
│   └── <Funcionalidad>/
├── Migrations/
└── wwwroot/
```

Cada elemento debe ubicarse en la carpeta correspondiente a su responsabilidad.

Las entidades y sus enumeraciones se ubican directamente en `Models/` y `Models/Enums/`.

`Views/`, `ViewModels/` y `Services/` se organizan con una subcarpeta por funcionalidad. Esa subcarpeta lleva el mismo nombre que el controlador sin el sufijo `Controller`. Por ejemplo, `SolicitudesController` utiliza `Views/Solicitudes/`, `ViewModels/Solicitudes/` y `Services/Solicitudes/`. No se ubican ViewModels ni Services sueltos directamente dentro de `ViewModels/` o `Services/`.

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
Diagnostico
Solicitud
Asignacion
Entrega
ConfiguracionInstitucional
RegistroAuditoria
ImagenEquipo
```

Las entidades se ubican en:

```text
Models/
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

Cada Service se define mediante una interfaz `I<Nombre>Service` y su implementación `<Nombre>Service`, ubicadas juntas en la subcarpeta de su funcionalidad:

```text
Services/<Funcionalidad>/
├── I<Nombre>Service.cs
└── <Nombre>Service.cs
```

Los Services contienen la lógica de negocio que no corresponde directamente al Controller.

Por ejemplo:

* validación de reglas de negocio;
* cambios de estado;
* coordinación de varias operaciones;
* transacciones;
* generación de registros de auditoría.

Los Controllers no deben concentrar la lógica de negocio.

Los servicios transversales utilizan su propia subcarpeta, por ejemplo `Services/Auditoria/` y `Services/Email/`.

## 7. ViewModels

Los ViewModels utilizan el nombre de la pantalla u operación seguido de `ViewModel`.

Ejemplos:

```text
RegistroDonacionViewModel
EdicionUsuarioViewModel
SolicitudDetalleViewModel
DiagnosticoInicialViewModel
ConfiguracionInstitucionalViewModel
AuditoriaFiltroViewModel
```

Se ubican en la subcarpeta de su funcionalidad:

```text
ViewModels/<Funcionalidad>/
```

Los ViewModels deben contener únicamente los datos necesarios para una determinada vista u operación.

No se debe utilizar una entidad de dominio como modelo de un formulario cuando el formulario requiere un conjunto diferente de campos.

## 8. Controllers

Los Controllers utilizan el nombre del recurso en plural seguido de `Controller`. Los que no corresponden a una entidad utilizan un nombre que describa su función.

Ejemplos:

```text
UsuariosController
PersonalController
EquiposController
DonacionesController
DiagnosticosController
SolicitudesController
AsignacionesController
EntregasController
AccountController
CatalogoController
ConfiguracionController
AuditoriaController
LegajoEquipoController
ReporteController
InicioController
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

No se crean controladores por rol. Las pantallas de cada rol se resuelven como acciones del controlador de la entidad, con la política de autorización correspondiente en cada acción.

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
    private readonly IDonacionService _donacionService;

    public DonacionesController(IDonacionService donacionService)
    {
        _donacionService = donacionService;
    }
}
```

Los Services se registran con su interfaz en `Program.cs`:

```csharp
builder.Services.AddScoped<IDonacionService, DonacionService>();
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

Cada tarea debe desarrollarse en una branch independiente, creada siempre a partir de `main` actualizado.

Convención:

```text
feature/<numero>-<descripcion>
fix/<numero>-<descripcion>
docs/<numero>-<descripcion>
```

`<numero>` es el número de la Issue de GitHub correspondiente. Cuando el cambio no tiene Issue, se omite el número.

Ejemplos:

```text
feature/91-entidad-equipo
feature/96-dbcontext
fix/validar-solicitud-duplicada
docs/actualizar-estructura-convenciones
```

Usar:

* `feature/` para funcionalidades nuevas, configuración y tareas técnicas;
* `fix/` para correcciones;
* `docs/` para documentación.

Los nombres deben ser descriptivos. No se utilizan nombres como `rama1`, `prueba`, `cosas` o `final`.

Antes de realizar commits, verificar la branch activa con `git branch`.

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

Antes de crear el Pull Request:

1. Ejecutar `dotnet build`.
2. Si existen pruebas, ejecutar `dotnet test`.
3. Verificar el estado con `git status`.

No se crea un Pull Request si el proyecto no compila.
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
