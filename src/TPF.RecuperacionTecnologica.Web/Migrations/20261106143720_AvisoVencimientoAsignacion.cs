using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPF.RecuperacionTecnologica.Web.Migrations
{
    /// <inheritdoc />
    public partial class AvisoVencimientoAsignacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAvisoVencimiento",
                table: "Asignaciones",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaAvisoVencimiento",
                table: "Asignaciones");
        }
    }
}
