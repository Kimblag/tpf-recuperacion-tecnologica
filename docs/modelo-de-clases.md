# Modelo UML de clases

## Convenciones

- Cada clase muestra sus propiedades escalares con el tipo del código, incluidas las claves foráneas (`NroEquipo`, `CodigoCoordinador`, etc.). Las propiedades de navegación (`Equipo`, `Coordinador`, etc.) se representan como asociaciones.
- `?` indica que la propiedad admite nulo.
- Las flechas salen de la clase que tiene la propiedad de navegación y apuntan a la clase referenciada. La multiplicidad junto a cada extremo indica cuántas instancias participan.
- `EspecificacionesTecnicas` es un objeto de valor (propiedad compleja de EF): no tiene identidad ni tabla propia y se guarda dentro de `Equipo` y de `Diagnostico`. Se representa con composición.
- `IdentityUserId` (en `Usuario` y `Personal`) referencia a la cuenta de ASP.NET Core Identity, que está fuera de este modelo.
- Las enumeraciones se listan aparte, en la sección de referencia.

## Diagrama

```mermaid
classDiagram
    direction TB

    class Usuario {
        +int NroUsuario
        +string IdentityUserId
        +TipoUsuario TipoUsuario
        +string? Nombre
        +string? Apellido
        +string? RazonSocial
        +TipoDocumento TipoDocumento
        +string NroDocumento
        +string? Direccion
        +string? Telefono
        +string CorreoElectronico
        +EstadoUsuario EstadoUsuario
        +DateTime FechaAlta
        +DateTime? FechaUltimaModificacion
        +DateTime? FechaBaja
        +string? MotivoBaja
    }

    class Personal {
        +int CodigoPersonal
        +string IdentityUserId
        +string Dni
        +string Nombre
        +string Apellido
        +string? Telefono
        +string CorreoElectronico
        +RolPersonal RolAsignado
        +EstadoPersonal EstadoPersonal
        +DateTime FechaAlta
        +DateTime? FechaUltimaModificacion
        +DateTime? FechaInactivacion
        +string? MotivoInactivacion
        +bool EsRaiz
    }

    class Equipo {
        +int NroEquipo
        +int NroUsuarioDonante
        +TipoEquipo TipoEquipo
        +string Marca
        +string Modelo
        +string? NumeroSerie
        +EstadoInicialDeclarado EstadoInicialDeclarado
        +string? Observaciones
        +EspecificacionesTecnicas EspecificacionesDeclaradas
        +EstadoEquipo EstadoEquipo
        +DateTime FechaRegistro
        +DateTime? FechaUltimaModificacion
        +DateTime? FechaDisponibilidad
        +DateTime? FechaCancelacion
        +string? MotivoCancelacion
        +string? ImagenUrl
        +byte[] RowVersion
    }

    class EspecificacionesTecnicas {
        <<valor>>
        +string? Procesador
        +string? MemoriaRam
        +string? Almacenamiento
        +string? TamanoPantalla
        +string? EstadoBateria
        +string? Otros
    }

    class Diagnostico {
        +int NroDiagnostico
        +int NroEquipo
        +int CodigoTecnico
        +TipoDiagnostico TipoDiagnostico
        +DateTime FechaDiagnostico
        +string? DetalleFallas
        +string? DetalleReparacionesRealizadas
        +string? Observaciones
        +DictamenTecnico DictamenTecnico
        +EspecificacionesTecnicas EspecificacionesVerificadas
        +int? DiagnosticoRectificadoId
        +bool EsVigente
    }

    class ImagenEquipo {
        +int NroImagen
        +int NroEquipo
        +int? NroDiagnostico
        +string UrlImagen
        +string? Angulo
        +DateTime FechaCarga
    }

    class Solicitud {
        +int NroSolicitud
        +int NroUsuario
        +TipoSolicitante TipoSolicitante
        +int NroEquipoRequerido
        +string MotivoSolicitud
        +OrigenSolicitud OrigenSolicitud
        +int? CodigoCoordinador
        +DateTime FechaSolicitud
        +DateTime? FechaUltimaModificacion
        +EstadoSolicitud EstadoSolicitud
        +string? MotivoCancelacion
        +DateTime? FechaCancelacion
        +DateTime? FechaCierre
        +string? MotivoCierre
        +int? CodigoCoordinadorCancelacion
        +byte[] RowVersion
    }

    class Asignacion {
        +int NroAsignacion
        +int NroEquipo
        +int NroSolicitud
        +int CodigoCoordinador
        +DateTime FechaAsignacion
        +DateTime FechaLimiteRetiro
        +DateTime? FechaUltimaModificacion
        +string? JustificacionCambio
        +EstadoAsignacion EstadoAsignacion
        +DateTime? FechaLiberacion
        +string? MotivoLiberacion
        +DateTime? FechaAvisoVencimiento
        +byte[] RowVersion
    }

    class Entrega {
        +int NroEntrega
        +int NroEquipo
        +int NroSolicitud
        +int NroAsignacion
        +int CodigoCoordinador
        +DateTime FechaEntrega
        +string NombreQuienRetira
        +string DocumentoIdentidadQuienRetira
    }

    class ConfiguracionInstitucional {
        +int Id
        +string NombreOrganizacion
        +string ZonaCobertura
        +int DiasLimiteRetiro
        +int MaximoSolicitudesPendientes
        +int DiasAvisoVencimiento
        +int CodigoAdministrador
        +DateTime? FechaUltimaModificacion
        +byte[] RowVersion
    }

    class RegistroAuditoria {
        +int NroAuditoria
        +DateTime FechaHora
        +TipoOperacionAuditoria TipoOperacion
        +int? CodigoPersonal
        +string EntidadAfectada
        +int IdEntidadAfectada
        +string? Detalle
    }

    Equipo "0..*" --> "1" Usuario : donante
    Solicitud "0..*" --> "1" Usuario : solicitante
    Solicitud "0..*" --> "1" Equipo : equipo requerido
    Diagnostico "0..*" --> "1" Equipo : evalúa
    Diagnostico "0..*" --> "1" Personal : técnico
    Diagnostico "0..*" --> "0..1" Diagnostico : rectifica
    ImagenEquipo "0..*" --> "1" Equipo : pertenece a
    ImagenEquipo "0..*" --> "0..1" Diagnostico : cargada en
    Asignacion "0..*" --> "1" Equipo : reserva
    Asignacion "0..*" --> "1" Solicitud : atiende
    Asignacion "0..*" --> "1" Personal : coordinador
    Entrega "0..1" --> "1" Asignacion : concreta
    Entrega "0..*" --> "1" Equipo : entrega
    Entrega "0..*" --> "1" Solicitud : cierra
    Entrega "0..*" --> "1" Personal : coordinador
    Solicitud "0..*" --> "0..1" Personal : coordinador (asistida)
    Solicitud "0..*" --> "0..1" Personal : coordinador que cancela
    ConfiguracionInstitucional "0..*" --> "1" Personal : administrador
    RegistroAuditoria "0..*" --> "0..1" Personal : realizada por

    Equipo "1" *-- "1" EspecificacionesTecnicas : declaradas
    Diagnostico "1" *-- "1" EspecificacionesTecnicas : verificadas
```

## Reglas del modelo que acotan las multiplicidades

Las multiplicidades del diagrama son las que permite el esquema. Estas restricciones de unicidad (índices únicos de la base) las limitan en la práctica:

- **Asignación vigente:** un equipo tiene como máximo una `Asignacion` con estado `Vigente`, y una solicitud también. Las liberadas y las concretadas se conservan como historial.
- **Entrega:** una asignación se entrega una sola vez (índice único sobre `NroAsignacion`).
- **Solicitud pendiente:** un usuario no puede tener dos solicitudes `Pendiente` para el mismo equipo.
- **Número de serie:** `NumeroSerie` es opcional y único entre los equipos que lo tienen.
- **Configuración institucional:** hay una única fila (`Id = 1`).
- **Borrado:** todas las relaciones restringen el borrado (`Restrict`), de modo que el historial no se pierde.

## Enumeraciones

| Enumeración | Valores |
|---|---|
| `TipoUsuario` | `Persona`, `Institucion` |
| `TipoDocumento` | `Dni`, `Cuit` |
| `EstadoUsuario` | `Activo`, `Inactivo` |
| `RolPersonal` | `Tecnico`, `Coordinador`, `Administrador` |
| `EstadoPersonal` | `Activo`, `Inactivo` |
| `TipoEquipo` | `Notebook`, `PcEscritorio`, `Monitor`, `Celular`, `Periferico` |
| `EstadoInicialDeclarado` | `Funcional`, `ConFallas`, `ParaRepuestos` |
| `EstadoEquipo` | `PendienteRecepcion`, `Disponible`, `EnReparacion`, `Asignado`, `Entregado`, `Baja`, `Cancelado` |
| `TipoDiagnostico` | `Inicial`, `Segundo`, `Rectificacion` |
| `DictamenTecnico` | `Funcional`, `NecesitaReparacion`, `NoRecuperable` |
| `TipoSolicitante` | `Particular`, `Escuela`, `OrganizacionSocial` |
| `OrigenSolicitud` | `Autoservicio`, `Asistida` |
| `EstadoSolicitud` | `Pendiente`, `Asignada`, `Completada`, `Cerrada`, `Cancelada` |
| `EstadoAsignacion` | `Vigente`, `Liberada`, `Concretada` |
| `TipoOperacionAuditoria` | `AltaUsuario`, `BajaUsuario`, `Donacion`, `Diagnostico`, `Solicitud`, `Asignacion`, `Liberacion`, `Entrega`, `AltaPersonal`, `InactivacionPersonal`, `ModificacionParametro`, `InicioSesion`, `CierreSesion`, `IntentoFallidoSesion`, `BloqueoCuenta`, `EdicionPersonal`, `ImagenEquipo`, `ActivacionCuenta`, `ReactivacionPersonal`, `IngresoSinRegistro`, `CorreccionUsuario` |
