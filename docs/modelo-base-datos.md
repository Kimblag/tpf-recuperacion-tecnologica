# Modelo de base de datos

Modelo relacional resultante en SQL Server (Entity Framework Core). Los tipos, longitudes, claves, índices y restricciones se tomaron de `src/TPF.RecuperacionTecnologica.Web/Data/Configurations` y de las migraciones (`Migrations/ApplicationDbContextModelSnapshot.cs`).

## Convenciones

- `PK` clave primaria, `FK` clave foránea, `UK` columna con índice único.
- Las claves primarias son `int` con `IDENTITY`, salvo `ConfiguracionInstitucional.Id`, que se asigna manualmente y vale siempre 1.
- Todas las claves foráneas usan `ON DELETE NO ACTION` (`DeleteBehavior.Restrict`): no hay borrado en cascada entre tablas de negocio.
- Las enumeraciones del dominio se guardan como `int`.
- Las columnas `RowVersion` son de tipo `rowversion` y se usan como token de concurrencia optimista (ver [Control de concurrencia](#control-de-concurrencia)).
- `EspecificacionesTecnicas` es un objeto de valor: no tiene tabla propia. Sus seis campos se guardan como columnas con prefijo en `Equipos` (`EspecificacionesDeclaradas_*`) y en `Diagnosticos` (`EspecificacionesVerificadas_*`).
- Las notaciones `o|` y `o{` del diagrama indican relación opcional (la clave foránea admite `NULL`).

## Diagrama entidad-relación

```mermaid
erDiagram
    Usuarios ||--o{ Equipos : "dona (NroUsuarioDonante)"
    Usuarios ||--o{ Solicitudes : "realiza (NroUsuario)"
    Equipos ||--o{ Solicitudes : "es requerido (NroEquipoRequerido)"
    Equipos ||--o{ Diagnosticos : "NroEquipo"
    Equipos ||--o{ ImagenesEquipo : "NroEquipo"
    Equipos ||--o{ Asignaciones : "NroEquipo"
    Equipos ||--o{ Entregas : "NroEquipo"
    Diagnosticos |o--o{ ImagenesEquipo : "NroDiagnostico"
    Diagnosticos |o--o{ Diagnosticos : "rectifica (DiagnosticoRectificadoId)"
    Solicitudes ||--o{ Asignaciones : "NroSolicitud"
    Solicitudes ||--o{ Entregas : "NroSolicitud"
    Asignaciones ||--o| Entregas : "NroAsignacion"
    PersonalInterno ||--o{ Diagnosticos : "CodigoTecnico"
    PersonalInterno ||--o{ Asignaciones : "CodigoCoordinador"
    PersonalInterno ||--o{ Entregas : "CodigoCoordinador"
    PersonalInterno |o--o{ Solicitudes : "CodigoCoordinador"
    PersonalInterno |o--o{ Solicitudes : "CodigoCoordinadorCancelacion"
    PersonalInterno ||--o{ ConfiguracionInstitucional : "CodigoAdministrador"
    PersonalInterno |o--o{ RegistrosAuditoria : "CodigoPersonal"

    Usuarios {
        int NroUsuario PK
        nvarchar_450 IdentityUserId UK "sin FK física a AspNetUsers"
        int TipoUsuario
        nvarchar_100 Nombre "NULL"
        nvarchar_100 Apellido "NULL"
        nvarchar_150 RazonSocial "NULL"
        int TipoDocumento
        nvarchar_11 NroDocumento UK
        nvarchar_200 Direccion "NULL"
        nvarchar_20 Telefono "NULL"
        nvarchar_254 CorreoElectronico UK
        int EstadoUsuario
        datetime2 FechaAlta
        datetime2 FechaUltimaModificacion "NULL"
        datetime2 FechaBaja "NULL"
        nvarchar_300 MotivoBaja "NULL"
    }

    PersonalInterno {
        int CodigoPersonal PK
        nvarchar_450 IdentityUserId UK "sin FK física a AspNetUsers"
        nvarchar_8 Dni UK
        nvarchar_100 Nombre
        nvarchar_100 Apellido
        nvarchar_254 CorreoElectronico UK
        nvarchar_20 Telefono "NULL"
        int RolAsignado
        int EstadoPersonal
        bit EsRaiz "UK filtrado (EsRaiz = 1)"
        datetime2 FechaAlta
        datetime2 FechaUltimaModificacion "NULL"
        datetime2 FechaInactivacion "NULL"
        nvarchar_300 MotivoInactivacion "NULL"
        rowversion RowVersion "concurrencia"
    }

    Equipos {
        int NroEquipo PK
        int NroUsuarioDonante FK
        int TipoEquipo
        nvarchar_60 Marca
        nvarchar_60 Modelo
        nvarchar_50 NumeroSerie UK "NULL, único si no es nulo"
        nvarchar_30 EspecificacionesDeclaradas_Almacenamiento "NULL"
        nvarchar_100 EspecificacionesDeclaradas_EstadoBateria "NULL"
        nvarchar_30 EspecificacionesDeclaradas_MemoriaRam "NULL"
        nvarchar_300 EspecificacionesDeclaradas_Otros "NULL"
        nvarchar_100 EspecificacionesDeclaradas_Procesador "NULL"
        nvarchar_30 EspecificacionesDeclaradas_TamanoPantalla "NULL"
        int EstadoInicialDeclarado
        int EstadoEquipo
        nvarchar_500 Observaciones "NULL"
        nvarchar_500 ImagenUrl "NULL"
        datetime2 FechaRegistro
        datetime2 FechaUltimaModificacion "NULL"
        datetime2 FechaDisponibilidad "NULL"
        datetime2 FechaCancelacion "NULL"
        nvarchar_300 MotivoCancelacion "NULL"
        rowversion RowVersion "concurrencia"
    }

    Diagnosticos {
        int NroDiagnostico PK
        int NroEquipo FK
        int CodigoTecnico FK
        int DiagnosticoRectificadoId FK "NULL"
        int TipoDiagnostico
        int DictamenTecnico
        bit EsVigente
        datetime2 FechaDiagnostico
        nvarchar_500 DetalleFallas "NULL"
        nvarchar_500 DetalleReparacionesRealizadas "NULL"
        nvarchar_500 Observaciones "NULL"
        nvarchar_30 EspecificacionesVerificadas_Almacenamiento "NULL"
        nvarchar_100 EspecificacionesVerificadas_EstadoBateria "NULL"
        nvarchar_30 EspecificacionesVerificadas_MemoriaRam "NULL"
        nvarchar_300 EspecificacionesVerificadas_Otros "NULL"
        nvarchar_100 EspecificacionesVerificadas_Procesador "NULL"
        nvarchar_30 EspecificacionesVerificadas_TamanoPantalla "NULL"
    }

    ImagenesEquipo {
        int NroImagen PK
        int NroEquipo FK
        int NroDiagnostico FK "NULL"
        nvarchar_500 UrlImagen
        nvarchar_40 Angulo "NULL"
        datetime2 FechaCarga
    }

    Solicitudes {
        int NroSolicitud PK
        int NroUsuario FK
        int NroEquipoRequerido FK
        int CodigoCoordinador FK "NULL"
        int CodigoCoordinadorCancelacion FK "NULL"
        int TipoSolicitante
        int OrigenSolicitud
        int EstadoSolicitud
        nvarchar_500 MotivoSolicitud
        datetime2 FechaSolicitud
        datetime2 FechaUltimaModificacion "NULL"
        datetime2 FechaCierre "NULL"
        nvarchar_300 MotivoCierre "NULL"
        datetime2 FechaCancelacion "NULL"
        nvarchar_300 MotivoCancelacion "NULL"
        rowversion RowVersion "concurrencia"
    }

    Asignaciones {
        int NroAsignacion PK
        int NroSolicitud FK
        int NroEquipo FK
        int CodigoCoordinador FK
        int EstadoAsignacion
        datetime2 FechaAsignacion
        datetime2 FechaLimiteRetiro
        datetime2 FechaAvisoVencimiento "NULL"
        datetime2 FechaLiberacion "NULL"
        nvarchar_300 MotivoLiberacion "NULL"
        nvarchar_500 JustificacionCambio "NULL"
        datetime2 FechaUltimaModificacion "NULL"
        rowversion RowVersion "concurrencia"
    }

    Entregas {
        int NroEntrega PK
        int NroAsignacion FK "UK"
        int NroSolicitud FK
        int NroEquipo FK
        int CodigoCoordinador FK
        nvarchar_100 NombreQuienRetira
        nvarchar_20 DocumentoIdentidadQuienRetira
        datetime2 FechaEntrega
    }

    RegistrosAuditoria {
        int NroAuditoria PK
        int CodigoPersonal FK "NULL"
        int TipoOperacion
        nvarchar_60 EntidadAfectada
        int IdEntidadAfectada "sin FK (referencia genérica)"
        nvarchar_500 Detalle "NULL"
        datetime2 FechaHora
    }

    ConfiguracionInstitucional {
        int Id PK "siempre 1"
        int CodigoAdministrador FK
        nvarchar_150 NombreOrganizacion
        nvarchar_150 ZonaCobertura
        int DiasLimiteRetiro
        int DiasAvisoVencimiento
        int MaximoSolicitudesPendientes
        datetime2 FechaUltimaModificacion "NULL"
        rowversion RowVersion "concurrencia"
    }
```

> En el diagrama, Mermaid no admite paréntesis en los tipos, por lo que `nvarchar_100` equivale a `nvarchar(100)`.

## Relación con ASP.NET Core Identity

Las tablas de ASP.NET Core Identity (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, `AspNetUserTokens`) **no se incluyen** en el diagrama.

`Usuarios` y `PersonalInterno` se relacionan con `AspNetUsers` de forma **indirecta**: ambas tablas guardan en `IdentityUserId` (`nvarchar(450)`, único) el `Id` de la cuenta de Identity. **No existe clave foránea física** entre ellas; la integridad de esa referencia la garantiza la aplicación.

## Claves foráneas

| Tabla origen | Columna | Tabla destino | Obligatoria |
|---|---|---|---|
| Equipos | NroUsuarioDonante | Usuarios | Sí |
| Solicitudes | NroUsuario | Usuarios | Sí |
| Solicitudes | NroEquipoRequerido | Equipos | Sí |
| Solicitudes | CodigoCoordinador | PersonalInterno | No |
| Solicitudes | CodigoCoordinadorCancelacion | PersonalInterno | No |
| Diagnosticos | NroEquipo | Equipos | Sí |
| Diagnosticos | CodigoTecnico | PersonalInterno | Sí |
| Diagnosticos | DiagnosticoRectificadoId | Diagnosticos (autorreferencia) | No |
| ImagenesEquipo | NroEquipo | Equipos | Sí |
| ImagenesEquipo | NroDiagnostico | Diagnosticos | No |
| Asignaciones | NroSolicitud | Solicitudes | Sí |
| Asignaciones | NroEquipo | Equipos | Sí |
| Asignaciones | CodigoCoordinador | PersonalInterno | Sí |
| Entregas | NroAsignacion | Asignaciones | Sí |
| Entregas | NroSolicitud | Solicitudes | Sí |
| Entregas | NroEquipo | Equipos | Sí |
| Entregas | CodigoCoordinador | PersonalInterno | Sí |
| RegistrosAuditoria | CodigoPersonal | PersonalInterno | No |
| ConfiguracionInstitucional | CodigoAdministrador | PersonalInterno | Sí |

## Índices únicos y restricciones

| Tabla | Regla |
|---|---|
| Usuarios | Único en `IdentityUserId`, `NroDocumento` y `CorreoElectronico`. `CK_Usuario_NroDocumento_SoloDigitos`; `CK_Usuario_FechaBaja_Coherente` (`FechaBaja` nula o ≥ `FechaAlta`). |
| PersonalInterno | Único en `IdentityUserId`, `Dni` y `CorreoElectronico`. Índice único filtrado en `EsRaiz` donde `EsRaiz = 1` (un único administrador raíz). `CK_Personal_Dni_SoloDigitos`. |
| Equipos | Índice único filtrado en `NumeroSerie` donde no es nulo. |
| Solicitudes | Índice único filtrado en (`NroUsuario`, `NroEquipoRequerido`) donde `EstadoSolicitud = 0` (pendiente): un usuario no puede tener dos solicitudes pendientes para el mismo equipo. |
| Asignaciones | Índices únicos filtrados en `NroEquipo` y en `NroSolicitud` donde `EstadoAsignacion = 0` (vigente). `CK_Asignacion_FechaLiberacion_Coherente`; `CK_Asignacion_FechaLimiteRetiro_Coherente`. |
| Entregas | Único en `NroAsignacion`: una asignación se entrega una sola vez. |
| ConfiguracionInstitucional | `CK_ConfiguracionInstitucional_Id_Unico` (`Id = 1`): una única fila de configuración. |

## Control de concurrencia

Cinco tablas incluyen una columna `RowVersion` de tipo `rowversion` (8 bytes, `NOT NULL`, generada y actualizada por SQL Server en cada modificación). EF Core la usa como token de concurrencia optimista y lanza `DbUpdateConcurrencyException` si la fila cambió desde que se leyó.

| Tabla | Columna | Tipo |
|---|---|---|
| PersonalInterno | RowVersion | `rowversion` |
| Equipos | RowVersion | `rowversion` |
| Solicitudes | RowVersion | `rowversion` |
| Asignaciones | RowVersion | `rowversion` |
| ConfiguracionInstitucional | RowVersion | `rowversion` |
