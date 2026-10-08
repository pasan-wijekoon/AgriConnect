using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.src.migrations
{
    /// <inheritdoc />
    public partial class AddListingPriceQuantityCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Listing_MinPrice",
                table: "Listings",
                sql: "\"MinPrice\" IS NULL OR \"MinPrice\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listing_Quantity",
                table: "Listings",
                sql: "\"Quantity\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Listing_MinPrice",
                table: "Listings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Listing_Quantity",
                table: "Listings");
        }
    }
}
