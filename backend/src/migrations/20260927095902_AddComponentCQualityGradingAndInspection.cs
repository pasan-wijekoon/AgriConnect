using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.src.migrations
{
    /// <inheritdoc />
    public partial class AddComponentCQualityGradingAndInspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Notification",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClaimedGrade",
                table: "Listings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(5)",
                oldMaxLength: 5);

            migrationBuilder.CreateTable(
                name: "AgentWorkflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggerType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TriggerEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectiveText = table.Column<string>(type: "text", nullable: false),
                    PlanSteps = table.Column<string>(type: "jsonb", nullable: false),
                    ToolCallLog = table.Column<string>(type: "jsonb", nullable: false),
                    ValidationResult = table.Column<string>(type: "jsonb", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ApprovedByOfficerId = table.Column<Guid>(type: "uuid", nullable: true),
                    FinalOutcome = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentWorkflow", x => x.Id);
                    table.CheckConstraint("CK_AgentWorkflow_ApprovalStatus", "\"ApprovalStatus\" IN ('Pending','Approved','Rejected','RevisionRequested')");
                    table.ForeignKey(
                        name: "FK_AgentWorkflow_User_ApprovedByOfficerId",
                        column: x => x.ApprovedByOfficerId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Inspection",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfficerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmedGrade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    InspectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inspection_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inspection_User_OfficerId",
                        column: x => x.OfficerId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GradeDiscrepancyFlag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClaimedGrade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConfirmedGrade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FlaggedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "text", nullable: true),
                    ResolvedByOfficerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeDiscrepancyFlag", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeDiscrepancyFlag_Inspection_InspectionId",
                        column: x => x.InspectionId,
                        principalTable: "Inspection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradeDiscrepancyFlag_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradeDiscrepancyFlag_User_ResolvedByOfficerId",
                        column: x => x.ResolvedByOfficerId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InspectionPhoto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionPhoto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionPhoto_Inspection_InspectionId",
                        column: x => x.InspectionId,
                        principalTable: "Inspection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflow_ApprovalStatus",
                table: "AgentWorkflow",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflow_ApprovedByOfficerId",
                table: "AgentWorkflow",
                column: "ApprovedByOfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflow_TriggerEntityId",
                table: "AgentWorkflow",
                column: "TriggerEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeDiscrepancyFlag_InspectionId",
                table: "GradeDiscrepancyFlag",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeDiscrepancyFlag_ListingId",
                table: "GradeDiscrepancyFlag",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeDiscrepancyFlag_ResolvedByOfficerId",
                table: "GradeDiscrepancyFlag",
                column: "ResolvedByOfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspection_ListingId",
                table: "Inspection",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspection_OfficerId",
                table: "Inspection",
                column: "OfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionPhoto_InspectionId",
                table: "InspectionPhoto",
                column: "InspectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Listings_User_FarmerId",
                table: "Listings",
                column: "FarmerId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Listings_User_FarmerId",
                table: "Listings");

            migrationBuilder.DropTable(
                name: "AgentWorkflow");

            migrationBuilder.DropTable(
                name: "GradeDiscrepancyFlag");

            migrationBuilder.DropTable(
                name: "InspectionPhoto");

            migrationBuilder.DropTable(
                name: "Inspection");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Notification");

            migrationBuilder.AlterColumn<string>(
                name: "ClaimedGrade",
                table: "Listings",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
