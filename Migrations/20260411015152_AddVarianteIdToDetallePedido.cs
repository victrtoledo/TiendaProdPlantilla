using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiendaApi.Migrations
{
    /// <inheritdoc />
    public partial class AddVarianteIdToDetallePedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VarianteId",
                table: "DetallePedidos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetallePedidos_VarianteId",
                table: "DetallePedidos",
                column: "VarianteId");

            migrationBuilder.AddForeignKey(
                name: "FK_DetallePedidos_VariantesProducto_VarianteId",
                table: "DetallePedidos",
                column: "VarianteId",
                principalTable: "VariantesProducto",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DetallePedidos_VariantesProducto_VarianteId",
                table: "DetallePedidos");

            migrationBuilder.DropIndex(
                name: "IX_DetallePedidos_VarianteId",
                table: "DetallePedidos");

            migrationBuilder.DropColumn(
                name: "VarianteId",
                table: "DetallePedidos");
        }
    }
}
