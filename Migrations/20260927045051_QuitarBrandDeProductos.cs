using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApp.Migrations
{
    /// <inheritdoc />
    public partial class QuitarBrandDeProductos : Migration
    {
        /// <inheritdoc />
        // El texto Brand ya se copió a la tabla Marcas (y a Products.MarcaId) en
        // AgregarProveedoresYAbastecimientos; ahora el catálogo y los formularios usan MarcaId.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Brand",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "Products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Al revertir, se recupera el texto desde la marca enlazada.
            migrationBuilder.Sql(@"
                UPDATE ""Products"" p
                SET ""Brand"" = m.""Nombre""
                FROM ""Marcas"" m
                WHERE m.""Id"" = p.""MarcaId"";
            ");
        }
    }
}
