using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApp.Migrations
{
    /// <inheritdoc />
    public partial class ExtendServiceModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Difficulty",
                table: "Services",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Services",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "DifficultyDescription",
                table: "Services",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DurationHours",
                table: "Services",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndLatitude",
                table: "Services",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndLongitude",
                table: "Services",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndPoint",
                table: "Services",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Excludes",
                table: "Services",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GalleryImages",
                table: "Services",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GuideId",
                table: "Services",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Includes",
                table: "Services",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "Services",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxGroupSize",
                table: "Services",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PriceDescription",
                table: "Services",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Recommendations",
                table: "Services",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Services",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryImageUrl",
                table: "Services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortDescription",
                table: "Services",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartLatitude",
                table: "Services",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartLongitude",
                table: "Services",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartPoint",
                table: "Services",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TransportId",
                table: "Services",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Services_GuideId",
                table: "Services",
                column: "GuideId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_TransportId",
                table: "Services",
                column: "TransportId");

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Guides_GuideId",
                table: "Services",
                column: "GuideId",
                principalTable: "Guides",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Transports_TransportId",
                table: "Services",
                column: "TransportId",
                principalTable: "Transports",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Services_Guides_GuideId",
                table: "Services");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Transports_TransportId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_GuideId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_TransportId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "DifficultyDescription",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "DurationHours",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "EndLatitude",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "EndLongitude",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "EndPoint",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "Excludes",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "GalleryImages",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "GuideId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "Includes",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "MaxGroupSize",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "PriceDescription",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "Recommendations",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "SecondaryImageUrl",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "ShortDescription",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "StartLatitude",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "StartLongitude",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "StartPoint",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "TransportId",
                table: "Services");

            migrationBuilder.AlterColumn<string>(
                name: "Difficulty",
                table: "Services",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Services",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);
        }
    }
}
