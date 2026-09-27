using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcommerceApp.Migrations
{
    /// <inheritdoc />
    public partial class AgregarProveedoresYAbastecimientos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MarcaId",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockMinimo",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 5); // umbral de stock bajo que ya usaba la tienda

            migrationBuilder.CreateTable(
                name: "Marcas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marcas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Nit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PersonaContacto = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Ciudad = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Direccion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notas = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Abastecimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProveedorId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NumeroComprobante = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RegistradoPor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Abastecimientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Abastecimientos_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProveedorMarcas",
                columns: table => new
                {
                    ProveedorId = table.Column<int>(type: "integer", nullable: false),
                    MarcaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProveedorMarcas", x => new { x.ProveedorId, x.MarcaId });
                    table.ForeignKey(
                        name: "FK_ProveedorMarcas_Marcas_MarcaId",
                        column: x => x.MarcaId,
                        principalTable: "Marcas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProveedorMarcas_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetallesAbastecimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AbastecimientoId = table.Column<int>(type: "integer", nullable: false),
                    ProductoId = table.Column<int>(type: "integer", nullable: true),
                    NombreProducto = table.Column<string>(type: "text", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesAbastecimiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesAbastecimiento_Abastecimientos_AbastecimientoId",
                        column: x => x.AbastecimientoId,
                        principalTable: "Abastecimientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesAbastecimiento_Products_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_MarcaId",
                table: "Products",
                column: "MarcaId");

            migrationBuilder.CreateIndex(
                name: "IX_Abastecimientos_Fecha",
                table: "Abastecimientos",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Abastecimientos_ProveedorId",
                table: "Abastecimientos",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesAbastecimiento_AbastecimientoId",
                table: "DetallesAbastecimiento",
                column: "AbastecimientoId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesAbastecimiento_ProductoId",
                table: "DetallesAbastecimiento",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_Nombre",
                table: "Marcas",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Nombre",
                table: "Proveedores",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_ProveedorMarcas_MarcaId",
                table: "ProveedorMarcas",
                column: "MarcaId");

            // Migración de datos: una marca por cada texto distinto de Products.Brand
            // (sin distinguir mayúsculas ni espacios) y luego se enlaza cada producto con su marca.
            // La columna Brand se conserva por ahora; se elimina en la fase de marcas.
            migrationBuilder.Sql(@"
                INSERT INTO ""Marcas"" (""Nombre"", ""Activo"")
                SELECT MIN(LEFT(TRIM(""Brand""), 80)), TRUE
                FROM ""Products""
                WHERE ""Brand"" IS NOT NULL AND TRIM(""Brand"") <> ''
                GROUP BY LOWER(LEFT(TRIM(""Brand""), 80));

                UPDATE ""Products"" p
                SET ""MarcaId"" = m.""Id""
                FROM ""Marcas"" m
                WHERE p.""Brand"" IS NOT NULL
                  AND LOWER(LEFT(TRIM(p.""Brand""), 80)) = LOWER(m.""Nombre"");
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Marcas_MarcaId",
                table: "Products",
                column: "MarcaId",
                principalTable: "Marcas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Marcas_MarcaId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "DetallesAbastecimiento");

            migrationBuilder.DropTable(
                name: "ProveedorMarcas");

            migrationBuilder.DropTable(
                name: "Abastecimientos");

            migrationBuilder.DropTable(
                name: "Marcas");

            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropIndex(
                name: "IX_Products_MarcaId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MarcaId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StockMinimo",
                table: "Products");
        }
    }
}
