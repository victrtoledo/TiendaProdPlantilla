using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiendaApi.Migrations
{
    /// <inheritdoc />
    public partial class AddVarianteIdToCarrito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VarianteId",
                table: "CarritoItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarritoItems_VarianteId",
                table: "CarritoItems",
                column: "VarianteId");

            migrationBuilder.AddForeignKey(
                name: "FK_CarritoItems_VariantesProducto_VarianteId",
                table: "CarritoItems",
                column: "VarianteId",
                principalTable: "VariantesProducto",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarritoItems_VariantesProducto_VarianteId",
                table: "CarritoItems");

            migrationBuilder.DropIndex(
                name: "IX_CarritoItems_VarianteId",
                table: "CarritoItems");

            migrationBuilder.DropColumn(
                name: "VarianteId",
                table: "CarritoItems");
        }
    }
}
