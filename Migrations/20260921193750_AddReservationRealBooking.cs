using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcommerceApp.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationRealBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Id identity: Postgres numera automáticamente las filas existentes.
            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Reservations",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Reservations",
                type: "timestamp with time zone",
                nullable: true);

            // 2. TripDate nace nullable: todavía no hay dato para las filas existentes.
            migrationBuilder.AddColumn<DateOnly>(
                name: "TripDate",
                table: "Reservations",
                type: "date",
                nullable: true);

            // 3. Backfill: a las reservas existentes se les asigna como TripDate
            // la fecha (sin hora) de su BookingDate original.
            migrationBuilder.Sql(@"UPDATE ""Reservations"" SET ""TripDate"" = ""BookingDate""::date WHERE ""TripDate"" IS NULL;");

            // 4. Recién ahora se puede exigir NOT NULL: ya no quedan filas sin dato.
            migrationBuilder.AlterColumn<DateOnly>(
                name: "TripDate",
                table: "Reservations",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            // 5. Cambio de PK: sale (UserId, ServiceId), entra Id.
            migrationBuilder.DropPrimaryKey(
                name: "PK_Reservations",
                table: "Reservations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reservations",
                table: "Reservations",
                column: "Id");

            // 6. Único (UserId, ServiceId, TripDate).
            migrationBuilder.CreateIndex(
                name: "IX_Reservations_UserId_ServiceId_TripDate",
                table: "Reservations",
                columns: new[] { "UserId", "ServiceId", "TripDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Reservations",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_UserId_ServiceId_TripDate",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "TripDate",
                table: "Reservations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reservations",
                table: "Reservations",
                columns: new[] { "UserId", "ServiceId" });
        }
    }
}
