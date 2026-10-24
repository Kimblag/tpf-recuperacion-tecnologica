using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPF.RecuperacionTecnologica.Web.Migrations
{
    /// <inheritdoc />
    public partial class IndiceUnicoSolicitudPendiente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Solicitudes_NroUsuario",
                table: "Solicitudes");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_NroUsuario_NroEquipoRequerido",
                table: "Solicitudes",
                columns: new[] { "NroUsuario", "NroEquipoRequerido" },
                unique: true,
                filter: "[EstadoSolicitud] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Solicitudes_NroUsuario_NroEquipoRequerido",
                table: "Solicitudes");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_NroUsuario",
                table: "Solicitudes",
                column: "NroUsuario");
        }
    }
}
