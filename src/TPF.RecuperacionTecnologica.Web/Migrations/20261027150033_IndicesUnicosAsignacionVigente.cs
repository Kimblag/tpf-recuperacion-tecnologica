using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPF.RecuperacionTecnologica.Web.Migrations
{
    /// <inheritdoc />
    public partial class IndicesUnicosAsignacionVigente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Asignaciones_NroEquipo",
                table: "Asignaciones");

            migrationBuilder.DropIndex(
                name: "IX_Asignaciones_NroSolicitud",
                table: "Asignaciones");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_NroEquipo",
                table: "Asignaciones",
                column: "NroEquipo",
                unique: true,
                filter: "[EstadoAsignacion] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_NroSolicitud",
                table: "Asignaciones",
                column: "NroSolicitud",
                unique: true,
                filter: "[EstadoAsignacion] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Asignaciones_NroEquipo",
                table: "Asignaciones");

            migrationBuilder.DropIndex(
                name: "IX_Asignaciones_NroSolicitud",
                table: "Asignaciones");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_NroEquipo",
                table: "Asignaciones",
                column: "NroEquipo");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_NroSolicitud",
                table: "Asignaciones",
                column: "NroSolicitud");
        }
    }
}
