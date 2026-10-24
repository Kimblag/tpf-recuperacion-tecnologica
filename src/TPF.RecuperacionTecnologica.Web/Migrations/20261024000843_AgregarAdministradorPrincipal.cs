using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPF.RecuperacionTecnologica.Web.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAdministradorPrincipal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsRaiz",
                table: "PersonalInterno",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE [PersonalInterno] SET [EsRaiz] = 1 WHERE [CodigoPersonal] = (SELECT MIN([CodigoPersonal]) FROM [PersonalInterno] WHERE [RolAsignado] = 2)");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalInterno_EsRaiz",
                table: "PersonalInterno",
                column: "EsRaiz",
                unique: true,
                filter: "[EsRaiz] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PersonalInterno_EsRaiz",
                table: "PersonalInterno");

            migrationBuilder.DropColumn(
                name: "EsRaiz",
                table: "PersonalInterno");
        }
    }
}
