using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.src.migrations
{
    /// <inheritdoc />
    public partial class AddComponentDAnalyticsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriceAnomalyFlag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviationPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    FlaggedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceAnomalyFlag", x => x.Id);
                    table.CheckConstraint("CK_PriceAnomalyFlag_Status", "\"Status\" IN ('Open','Reviewed','Dismissed')");
                });

            migrationBuilder.CreateTable(
                name: "PriceTrendSnapshot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CropId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Period = table.Column<DateOnly>(type: "date", nullable: false),
                    AvgPrice = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    MinPrice = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    MaxPrice = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    SampleCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceTrendSnapshot", x => x.Id);
                    table.CheckConstraint("CK_PriceTrendSnapshot_SampleCount", "\"SampleCount\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "ReportExport",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DateRangeStart = table.Column<DateOnly>(type: "date", nullable: false),
                    DateRangeEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportExport", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShortageOversupplyEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CropId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Severity = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShortageOversupplyEvent", x => x.Id);
                    table.CheckConstraint("CK_ShortageOversupplyEvent_Severity", "\"Severity\" IN ('Low','Medium','High')");
                    table.CheckConstraint("CK_ShortageOversupplyEvent_Type", "\"Type\" IN ('Shortage','Oversupply')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceAnomalyFlag_ListingId",
                table: "PriceAnomalyFlag",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAnomalyFlag_Status",
                table: "PriceAnomalyFlag",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PriceTrendSnapshot_Crop_Region_Period",
                table: "PriceTrendSnapshot",
                columns: new[] { "CropId", "RegionId", "Period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportExport_RequestedBy",
                table: "ReportExport",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ShortageOversupplyEvent_Crop_Region",
                table: "ShortageOversupplyEvent",
                columns: new[] { "CropId", "RegionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceAnomalyFlag");

            migrationBuilder.DropTable(
                name: "PriceTrendSnapshot");

            migrationBuilder.DropTable(
                name: "ReportExport");

            migrationBuilder.DropTable(
                name: "ShortageOversupplyEvent");
        }
    }
}
