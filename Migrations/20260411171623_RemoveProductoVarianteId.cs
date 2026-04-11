using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiendaApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProductoVarianteId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductoVarianteId",
                table: "VariantesProducto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductoVarianteId",
                table: "VariantesProducto",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
