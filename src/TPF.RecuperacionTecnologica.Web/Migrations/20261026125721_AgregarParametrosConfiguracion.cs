using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPF.RecuperacionTecnologica.Web.Migrations
{
    /// <inheritdoc />
    public partial class AgregarParametrosConfiguracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiasAvisoVencimiento",
                table: "ConfiguracionInstitucional",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "MaximoSolicitudesPendientes",
                table: "ConfiguracionInstitucional",
                type: "int",
                nullable: false,
                defaultValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiasAvisoVencimiento",
                table: "ConfiguracionInstitucional");

            migrationBuilder.DropColumn(
                name: "MaximoSolicitudesPendientes",
                table: "ConfiguracionInstitucional");
        }
    }
}
