using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPF.RecuperacionTecnologica.Web.Migrations
{
    /// <inheritdoc />
    public partial class CoordinadorCancelacionSolicitud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CodigoCoordinadorCancelacion",
                table: "Solicitudes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_CodigoCoordinadorCancelacion",
                table: "Solicitudes",
                column: "CodigoCoordinadorCancelacion");

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitudes_PersonalInterno_CodigoCoordinadorCancelacion",
                table: "Solicitudes",
                column: "CodigoCoordinadorCancelacion",
                principalTable: "PersonalInterno",
                principalColumn: "CodigoPersonal",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Solicitudes_PersonalInterno_CodigoCoordinadorCancelacion",
                table: "Solicitudes");

            migrationBuilder.DropIndex(
                name: "IX_Solicitudes_CodigoCoordinadorCancelacion",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "CodigoCoordinadorCancelacion",
                table: "Solicitudes");
        }
    }
}
