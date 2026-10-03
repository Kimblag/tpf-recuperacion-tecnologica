using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPF.RecuperacionTecnologica.Web.Migrations
{
    /// <inheritdoc />
    public partial class InicialDominio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonalInterno",
                columns: table => new
                {
                    CodigoPersonal = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdentityUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Dni = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CorreoElectronico = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    RolAsignado = table.Column<int>(type: "int", nullable: false),
                    EstadoPersonal = table.Column<int>(type: "int", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    FechaUltimaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaInactivacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoInactivacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalInterno", x => x.CodigoPersonal);
                    table.CheckConstraint("CK_Personal_Dni_SoloDigitos", "[Dni] NOT LIKE '%[^0-9]%'");
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    NroUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdentityUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TipoUsuario = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RazonSocial = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TipoDocumento = table.Column<int>(type: "int", nullable: false),
                    NroDocumento = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CorreoElectronico = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    EstadoUsuario = table.Column<int>(type: "int", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    FechaUltimaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaBaja = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoBaja = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.NroUsuario);
                    table.CheckConstraint("CK_Usuario_FechaBaja_Coherente", "[FechaBaja] IS NULL OR [FechaBaja] >= [FechaAlta]");
                    table.CheckConstraint("CK_Usuario_NroDocumento_SoloDigitos", "[NroDocumento] NOT LIKE '%[^0-9]%'");
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionInstitucional",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    NombreOrganizacion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ZonaCobertura = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DiasLimiteRetiro = table.Column<int>(type: "int", nullable: false),
                    CodigoAdministrador = table.Column<int>(type: "int", nullable: false),
                    FechaUltimaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionInstitucional", x => x.Id);
                    table.CheckConstraint("CK_ConfiguracionInstitucional_Id_Unico", "[Id] = 1");
                    table.ForeignKey(
                        name: "FK_ConfiguracionInstitucional_PersonalInterno_CodigoAdministrador",
                        column: x => x.CodigoAdministrador,
                        principalTable: "PersonalInterno",
                        principalColumn: "CodigoPersonal",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosAuditoria",
                columns: table => new
                {
                    NroAuditoria = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    TipoOperacion = table.Column<int>(type: "int", nullable: false),
                    CodigoPersonal = table.Column<int>(type: "int", nullable: true),
                    EntidadAfectada = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    IdEntidadAfectada = table.Column<int>(type: "int", nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAuditoria", x => x.NroAuditoria);
                    table.ForeignKey(
                        name: "FK_RegistrosAuditoria_PersonalInterno_CodigoPersonal",
                        column: x => x.CodigoPersonal,
                        principalTable: "PersonalInterno",
                        principalColumn: "CodigoPersonal",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Equipos",
                columns: table => new
                {
                    NroEquipo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NroUsuarioDonante = table.Column<int>(type: "int", nullable: false),
                    TipoEquipo = table.Column<int>(type: "int", nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Modelo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EstadoInicialDeclarado = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EstadoEquipo = table.Column<int>(type: "int", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    FechaUltimaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaDisponibilidad = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCancelacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoCancelacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ImagenUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    EspecificacionesDeclaradas_Almacenamiento = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EspecificacionesDeclaradas_EstadoBateria = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EspecificacionesDeclaradas_MemoriaRam = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EspecificacionesDeclaradas_Otros = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EspecificacionesDeclaradas_Procesador = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EspecificacionesDeclaradas_TamanoPantalla = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipos", x => x.NroEquipo);
                    table.ForeignKey(
                        name: "FK_Equipos_Usuarios_NroUsuarioDonante",
                        column: x => x.NroUsuarioDonante,
                        principalTable: "Usuarios",
                        principalColumn: "NroUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Diagnosticos",
                columns: table => new
                {
                    NroDiagnostico = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NroEquipo = table.Column<int>(type: "int", nullable: false),
                    CodigoTecnico = table.Column<int>(type: "int", nullable: false),
                    TipoDiagnostico = table.Column<int>(type: "int", nullable: false),
                    FechaDiagnostico = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    DetalleFallas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DetalleReparacionesRealizadas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DictamenTecnico = table.Column<int>(type: "int", nullable: false),
                    DiagnosticoRectificadoId = table.Column<int>(type: "int", nullable: true),
                    EsVigente = table.Column<bool>(type: "bit", nullable: false),
                    EspecificacionesVerificadas_Almacenamiento = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EspecificacionesVerificadas_EstadoBateria = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EspecificacionesVerificadas_MemoriaRam = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EspecificacionesVerificadas_Otros = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EspecificacionesVerificadas_Procesador = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EspecificacionesVerificadas_TamanoPantalla = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diagnosticos", x => x.NroDiagnostico);
                    table.ForeignKey(
                        name: "FK_Diagnosticos_Diagnosticos_DiagnosticoRectificadoId",
                        column: x => x.DiagnosticoRectificadoId,
                        principalTable: "Diagnosticos",
                        principalColumn: "NroDiagnostico",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Diagnosticos_Equipos_NroEquipo",
                        column: x => x.NroEquipo,
                        principalTable: "Equipos",
                        principalColumn: "NroEquipo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Diagnosticos_PersonalInterno_CodigoTecnico",
                        column: x => x.CodigoTecnico,
                        principalTable: "PersonalInterno",
                        principalColumn: "CodigoPersonal",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Solicitudes",
                columns: table => new
                {
                    NroSolicitud = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NroUsuario = table.Column<int>(type: "int", nullable: false),
                    TipoSolicitante = table.Column<int>(type: "int", nullable: false),
                    NroEquipoRequerido = table.Column<int>(type: "int", nullable: false),
                    MotivoSolicitud = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OrigenSolicitud = table.Column<int>(type: "int", nullable: false),
                    CodigoCoordinador = table.Column<int>(type: "int", nullable: true),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    FechaUltimaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstadoSolicitud = table.Column<int>(type: "int", nullable: false),
                    MotivoCancelacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    FechaCancelacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitudes", x => x.NroSolicitud);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Equipos_NroEquipoRequerido",
                        column: x => x.NroEquipoRequerido,
                        principalTable: "Equipos",
                        principalColumn: "NroEquipo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solicitudes_PersonalInterno_CodigoCoordinador",
                        column: x => x.CodigoCoordinador,
                        principalTable: "PersonalInterno",
                        principalColumn: "CodigoPersonal",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Usuarios_NroUsuario",
                        column: x => x.NroUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "NroUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImagenesEquipo",
                columns: table => new
                {
                    NroImagen = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NroEquipo = table.Column<int>(type: "int", nullable: false),
                    NroDiagnostico = table.Column<int>(type: "int", nullable: true),
                    UrlImagen = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Angulo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    FechaCarga = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagenesEquipo", x => x.NroImagen);
                    table.ForeignKey(
                        name: "FK_ImagenesEquipo_Diagnosticos_NroDiagnostico",
                        column: x => x.NroDiagnostico,
                        principalTable: "Diagnosticos",
                        principalColumn: "NroDiagnostico",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImagenesEquipo_Equipos_NroEquipo",
                        column: x => x.NroEquipo,
                        principalTable: "Equipos",
                        principalColumn: "NroEquipo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Asignaciones",
                columns: table => new
                {
                    NroAsignacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NroEquipo = table.Column<int>(type: "int", nullable: false),
                    NroSolicitud = table.Column<int>(type: "int", nullable: false),
                    CodigoCoordinador = table.Column<int>(type: "int", nullable: false),
                    FechaAsignacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    FechaUltimaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JustificacionCambio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EstadoAsignacion = table.Column<int>(type: "int", nullable: false),
                    FechaLiberacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoLiberacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asignaciones", x => x.NroAsignacion);
                    table.CheckConstraint("CK_Asignacion_FechaLiberacion_Coherente", "[FechaLiberacion] IS NULL OR [FechaLiberacion] >= [FechaAsignacion]");
                    table.ForeignKey(
                        name: "FK_Asignaciones_Equipos_NroEquipo",
                        column: x => x.NroEquipo,
                        principalTable: "Equipos",
                        principalColumn: "NroEquipo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Asignaciones_PersonalInterno_CodigoCoordinador",
                        column: x => x.CodigoCoordinador,
                        principalTable: "PersonalInterno",
                        principalColumn: "CodigoPersonal",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Asignaciones_Solicitudes_NroSolicitud",
                        column: x => x.NroSolicitud,
                        principalTable: "Solicitudes",
                        principalColumn: "NroSolicitud",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Entregas",
                columns: table => new
                {
                    NroEntrega = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NroEquipo = table.Column<int>(type: "int", nullable: false),
                    NroSolicitud = table.Column<int>(type: "int", nullable: false),
                    NroAsignacion = table.Column<int>(type: "int", nullable: false),
                    CodigoCoordinador = table.Column<int>(type: "int", nullable: false),
                    FechaEntrega = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    DocumentoIdentidadQuienRetira = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entregas", x => x.NroEntrega);
                    table.ForeignKey(
                        name: "FK_Entregas_Asignaciones_NroAsignacion",
                        column: x => x.NroAsignacion,
                        principalTable: "Asignaciones",
                        principalColumn: "NroAsignacion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entregas_Equipos_NroEquipo",
                        column: x => x.NroEquipo,
                        principalTable: "Equipos",
                        principalColumn: "NroEquipo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entregas_PersonalInterno_CodigoCoordinador",
                        column: x => x.CodigoCoordinador,
                        principalTable: "PersonalInterno",
                        principalColumn: "CodigoPersonal",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entregas_Solicitudes_NroSolicitud",
                        column: x => x.NroSolicitud,
                        principalTable: "Solicitudes",
                        principalColumn: "NroSolicitud",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_CodigoCoordinador",
                table: "Asignaciones",
                column: "CodigoCoordinador");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_NroEquipo",
                table: "Asignaciones",
                column: "NroEquipo");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_NroSolicitud",
                table: "Asignaciones",
                column: "NroSolicitud");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionInstitucional_CodigoAdministrador",
                table: "ConfiguracionInstitucional",
                column: "CodigoAdministrador");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnosticos_CodigoTecnico",
                table: "Diagnosticos",
                column: "CodigoTecnico");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnosticos_DiagnosticoRectificadoId",
                table: "Diagnosticos",
                column: "DiagnosticoRectificadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnosticos_NroEquipo",
                table: "Diagnosticos",
                column: "NroEquipo");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_CodigoCoordinador",
                table: "Entregas",
                column: "CodigoCoordinador");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_NroAsignacion",
                table: "Entregas",
                column: "NroAsignacion");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_NroEquipo",
                table: "Entregas",
                column: "NroEquipo");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_NroSolicitud",
                table: "Entregas",
                column: "NroSolicitud");

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_NroUsuarioDonante",
                table: "Equipos",
                column: "NroUsuarioDonante");

            migrationBuilder.CreateIndex(
                name: "IX_ImagenesEquipo_NroDiagnostico",
                table: "ImagenesEquipo",
                column: "NroDiagnostico");

            migrationBuilder.CreateIndex(
                name: "IX_ImagenesEquipo_NroEquipo",
                table: "ImagenesEquipo",
                column: "NroEquipo");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalInterno_CorreoElectronico",
                table: "PersonalInterno",
                column: "CorreoElectronico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonalInterno_Dni",
                table: "PersonalInterno",
                column: "Dni",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonalInterno_IdentityUserId",
                table: "PersonalInterno",
                column: "IdentityUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_CodigoPersonal",
                table: "RegistrosAuditoria",
                column: "CodigoPersonal");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_CodigoCoordinador",
                table: "Solicitudes",
                column: "CodigoCoordinador");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_NroEquipoRequerido",
                table: "Solicitudes",
                column: "NroEquipoRequerido");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_NroUsuario",
                table: "Solicitudes",
                column: "NroUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CorreoElectronico",
                table: "Usuarios",
                column: "CorreoElectronico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdentityUserId",
                table: "Usuarios",
                column: "IdentityUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_NroDocumento",
                table: "Usuarios",
                column: "NroDocumento",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "ConfiguracionInstitucional");

            migrationBuilder.DropTable(
                name: "Entregas");

            migrationBuilder.DropTable(
                name: "ImagenesEquipo");

            migrationBuilder.DropTable(
                name: "RegistrosAuditoria");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Asignaciones");

            migrationBuilder.DropTable(
                name: "Diagnosticos");

            migrationBuilder.DropTable(
                name: "Solicitudes");

            migrationBuilder.DropTable(
                name: "Equipos");

            migrationBuilder.DropTable(
                name: "PersonalInterno");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
