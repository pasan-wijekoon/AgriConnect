using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.src.migrations
{
    /// <inheritdoc />
    public partial class AddOrderLocationAndReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BuyerReminderSentAt",
                table: "StockReservation",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OfficerReminderSentAt",
                table: "StockReservation",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BuyerLatitude",
                table: "Order",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BuyerLongitude",
                table: "Order",
                type: "numeric(5,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyerReminderSentAt",
                table: "StockReservation");

            migrationBuilder.DropColumn(
                name: "OfficerReminderSentAt",
                table: "StockReservation");

            migrationBuilder.DropColumn(
                name: "BuyerLatitude",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "BuyerLongitude",
                table: "Order");
        }
    }
}
