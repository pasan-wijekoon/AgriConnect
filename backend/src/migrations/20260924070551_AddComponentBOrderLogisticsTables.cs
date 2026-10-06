using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.src.migrations
{
    /// <inheritdoc />
    public partial class AddComponentBOrderLogisticsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectionCentre",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    RegionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionCentre", x => x.Id);
                    table.CheckConstraint("CK_CollectionCentre_Capacity", "\"Capacity\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "Order",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DeliveryPreference = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order", x => x.Id);
                    table.CheckConstraint("CK_Order_DeliveryPreference", "\"DeliveryPreference\" IN ('Pickup','Delivery')");
                    table.CheckConstraint("CK_Order_Quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_Order_Status", "\"Status\" IN ('Pending','Approved','Scheduled','Completed','Cancelled')");
                });

            migrationBuilder.CreateTable(
                name: "PickupSchedule",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CollectionCentreId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SlotEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickupSchedule", x => x.Id);
                    table.CheckConstraint("CK_PickupSchedule_SlotWindow", "\"SlotEnd\" > \"SlotStart\"");
                    table.CheckConstraint("CK_PickupSchedule_Status", "\"Status\" IN ('Proposed','Confirmed','Cancelled')");
                    table.ForeignKey(
                        name: "FK_PickupSchedule_CollectionCentre_CollectionCentreId",
                        column: x => x.CollectionCentreId,
                        principalTable: "CollectionCentre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickupSchedule_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockReservation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReservation", x => x.Id);
                    table.CheckConstraint("CK_StockReservation_ReservedQuantity", "\"ReservedQuantity\" > 0");
                    table.ForeignKey(
                        name: "FK_StockReservation_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionCentre_RegionId",
                table: "CollectionCentre",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_Order_BuyerId",
                table: "Order",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_Order_ListingId",
                table: "Order",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_Order_Status",
                table: "Order",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PickupSchedule_Centre_Slot_Confirmed",
                table: "PickupSchedule",
                columns: new[] { "CollectionCentreId", "SlotStart", "SlotEnd" },
                unique: true,
                filter: "\"Status\" = 'Confirmed'");

            migrationBuilder.CreateIndex(
                name: "IX_PickupSchedule_OrderId",
                table: "PickupSchedule",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockReservation_ExpiresAt",
                table: "StockReservation",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservation_ListingId",
                table: "StockReservation",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservation_OrderId",
                table: "StockReservation",
                column: "OrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PickupSchedule");

            migrationBuilder.DropTable(
                name: "StockReservation");

            migrationBuilder.DropTable(
                name: "CollectionCentre");

            migrationBuilder.DropTable(
                name: "Order");
        }
    }
}
