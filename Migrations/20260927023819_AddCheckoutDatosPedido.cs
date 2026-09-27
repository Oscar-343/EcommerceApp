using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutDatosPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CiudadEntrega",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoTransaccion",
                table: "Orders",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionEntrega",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailEntrega",
                table: "Orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetodoPago",
                table: "Orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreEntrega",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenciaEntrega",
                table: "Orders",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TarjetaMarca",
                table: "Orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TarjetaUltimos4",
                table: "Orders",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefonoEntrega",
                table: "Orders",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CiudadEntrega",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CodigoTransaccion",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DireccionEntrega",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EmailEntrega",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "MetodoPago",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "NombreEntrega",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReferenciaEntrega",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TarjetaMarca",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TarjetaUltimos4",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TelefonoEntrega",
                table: "Orders");
        }
    }
}
