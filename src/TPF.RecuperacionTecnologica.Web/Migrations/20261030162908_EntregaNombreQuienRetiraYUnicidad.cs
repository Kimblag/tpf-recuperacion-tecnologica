using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPF.RecuperacionTecnologica.Web.Migrations
{
    /// <inheritdoc />
    public partial class EntregaNombreQuienRetiraYUnicidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Entregas_NroAsignacion",
                table: "Entregas");

            migrationBuilder.AddColumn<string>(
                name: "NombreQuienRetira",
                table: "Entregas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_NroAsignacion",
                table: "Entregas",
                column: "NroAsignacion",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Entregas_NroAsignacion",
                table: "Entregas");

            migrationBuilder.DropColumn(
                name: "NombreQuienRetira",
                table: "Entregas");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_NroAsignacion",
                table: "Entregas",
                column: "NroAsignacion");
        }
    }
}
