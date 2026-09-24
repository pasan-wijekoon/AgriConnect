using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.src.migrations
{
    /// <inheritdoc />
    public partial class AddAnomalyFlagListingContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CropId",
                table: "PriceAnomalyFlag",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "ListingPrice",
                table: "PriceAnomalyFlag",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "RegionId",
                table: "PriceAnomalyFlag",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PriceAnomalyFlag_CropId",
                table: "PriceAnomalyFlag",
                column: "CropId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PriceAnomalyFlag_CropId",
                table: "PriceAnomalyFlag");

            migrationBuilder.DropColumn(
                name: "CropId",
                table: "PriceAnomalyFlag");

            migrationBuilder.DropColumn(
                name: "ListingPrice",
                table: "PriceAnomalyFlag");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "PriceAnomalyFlag");
        }
    }
}
