using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPF.RecuperacionTecnologica.Web.Migrations
{
    /// <inheritdoc />
    public partial class AgregarFechaLimiteRetiroAsignacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaLimiteRetiro",
                table: "Asignaciones",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql(@"
                UPDATE Asignaciones
                SET FechaLimiteRetiro = DATEADD(
                    DAY,
                    COALESCE((SELECT TOP 1 DiasLimiteRetiro FROM ConfiguracionInstitucional), 10),
                    FechaAsignacion);");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Asignacion_FechaLimiteRetiro_Coherente",
                table: "Asignaciones",
                sql: "[FechaLimiteRetiro] >= [FechaAsignacion]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Asignacion_FechaLimiteRetiro_Coherente",
                table: "Asignaciones");

            migrationBuilder.DropColumn(
                name: "FechaLimiteRetiro",
                table: "Asignaciones");
        }
    }
}
