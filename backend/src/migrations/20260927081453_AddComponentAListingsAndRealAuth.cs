using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace backend.src.migrations
{
    /// <inheritdoc />
    public partial class AddComponentAListingsAndRealAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "User",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "User",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "User",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "User",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "User",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "User",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CollectionCentreId",
                table: "Region",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Crop",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CropId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Unit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ClaimedGrade = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    PickupWindowStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PickupWindowEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MinPrice = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Listings_Crop_CropId",
                        column: x => x.CropId,
                        principalTable: "Crop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Listings_Region_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Region",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TodayPriceCatalogItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Unit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DefaultRegion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodayPriceCatalogItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ListingPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListingPhotos_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    SuggestedPriceMin = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    SuggestedPriceMax = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(4,3)", nullable: false),
                    ReasoningSummary = table.Column<string>(type: "text", nullable: false),
                    AgentWorkflowId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AutoRejectedByValidation = table.Column<bool>(type: "boolean", nullable: false),
                    ValidationSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CheckpointName = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OfficerNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceSuggestions_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Crop",
                columns: new[] { "Id", "Category", "CreatedAt", "Name" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-0000-0000-000000000001"), "Grains", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Rice" },
                    { new Guid("a1000000-0000-0000-0000-000000000002"), "Beverages", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Tea" },
                    { new Guid("a1000000-0000-0000-0000-000000000003"), "Fruits", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Coconut" },
                    { new Guid("a1000000-0000-0000-0000-000000000004"), "Vegetables", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Tomatoes" },
                    { new Guid("a1000000-0000-0000-0000-000000000005"), "Vegetables", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Carrots" },
                    { new Guid("a1000000-0000-0000-0000-000000000006"), "Spices", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Chili" },
                    { new Guid("a1000000-0000-0000-0000-000000000007"), "Vegetables", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Onions" },
                    { new Guid("a1000000-0000-0000-0000-000000000008"), "Vegetables", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Potatoes" },
                    { new Guid("a1000000-0000-0000-0000-000000000009"), "Spices", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Cinnamon" },
                    { new Guid("a1000000-0000-0000-0000-00000000000a"), "Fruits", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Banana" }
                });

            migrationBuilder.InsertData(
                table: "Region",
                columns: new[] { "Id", "CollectionCentreId", "CreatedAt", "Name" },
                values: new object[,]
                {
                    { new Guid("b1000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Colombo" },
                    { new Guid("b1000000-0000-0000-0000-000000000002"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Kandy" },
                    { new Guid("b1000000-0000-0000-0000-000000000003"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Galle" },
                    { new Guid("b1000000-0000-0000-0000-000000000004"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Jaffna" },
                    { new Guid("b1000000-0000-0000-0000-000000000005"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Anuradhapura" },
                    { new Guid("b1000000-0000-0000-0000-000000000006"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Matara" },
                    { new Guid("b1000000-0000-0000-0000-000000000007"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Kurunegala" },
                    { new Guid("b1000000-0000-0000-0000-000000000008"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Nuwara Eliya" },
                    { new Guid("b1000000-0000-0000-0000-000000000009"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Gampaha" },
                    { new Guid("b1000000-0000-0000-0000-00000000000a"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Kalutara" },
                    { new Guid("b1000000-0000-0000-0000-00000000000b"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Matale" },
                    { new Guid("b1000000-0000-0000-0000-00000000000c"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Ratnapura" },
                    { new Guid("b1000000-0000-0000-0000-00000000000d"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Kegalle" },
                    { new Guid("b1000000-0000-0000-0000-00000000000e"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Badulla" },
                    { new Guid("b1000000-0000-0000-0000-00000000000f"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Monaragala" },
                    { new Guid("b1000000-0000-0000-0000-000000000010"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Hambantota" },
                    { new Guid("b1000000-0000-0000-0000-000000000011"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Trincomalee" },
                    { new Guid("b1000000-0000-0000-0000-000000000012"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Batticaloa" },
                    { new Guid("b1000000-0000-0000-0000-000000000013"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Ampara" },
                    { new Guid("b1000000-0000-0000-0000-000000000014"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Puttalam" },
                    { new Guid("b1000000-0000-0000-0000-000000000015"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Polonnaruwa" },
                    { new Guid("b1000000-0000-0000-0000-000000000016"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Kilinochchi" },
                    { new Guid("b1000000-0000-0000-0000-000000000017"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Mannar" },
                    { new Guid("b1000000-0000-0000-0000-000000000018"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Mullaitivu" },
                    { new Guid("b1000000-0000-0000-0000-000000000019"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Vavuniya" }
                });

            migrationBuilder.InsertData(
                table: "TodayPriceCatalogItems",
                columns: new[] { "Id", "Category", "CreatedAt", "DefaultRegion", "DisplayOrder", "ImageUrl", "IsActive", "Name", "Unit", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("d0000000-0000-0000-0000-000000000001"), "Vegetables", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dambulla", 1, "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?w=500&auto=format&fit=crop", true, "Tomatoes", "kg", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0000000-0000-0000-0000-000000000002"), "Vegetables", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nuwara Eliya", 2, "https://images.unsplash.com/photo-1598170845058-32b9d6a5c317?w=500&auto=format&fit=crop", true, "Carrots", "kg", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0000000-0000-0000-0000-000000000003"), "Vegetables", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nuwara Eliya", 3, "https://images.unsplash.com/photo-1518977676601-b53f82aba655?w=500&auto=format&fit=crop", true, "Potatoes", "kg", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0000000-0000-0000-0000-000000000004"), "Vegetables", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dambulla", 4, "https://images.unsplash.com/photo-1618512496248-a07fe83aa8cb?w=500&auto=format&fit=crop", true, "Onions", "kg", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0000000-0000-0000-0000-000000000005"), "Spices", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Jaffna", 5, "https://images.unsplash.com/photo-1588252303782-cb80119abd6d?w=500&auto=format&fit=crop", true, "Chili", "kg", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0000000-0000-0000-0000-000000000006"), "Grains", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Anuradhapura", 6, "https://images.unsplash.com/photo-1586201375761-83865001e31c?w=500&auto=format&fit=crop", true, "Rice (Keeri Samba)", "kg", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0000000-0000-0000-0000-000000000007"), "Fruits", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kurunegala", 7, "https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=500&auto=format&fit=crop", true, "Banana (Ambul)", "kg", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0000000-0000-0000-0000-000000000008"), "Fruits", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kurunegala", 8, "https://images.unsplash.com/photo-1544376798-89aa6b82c6cd?w=500&auto=format&fit=crop", true, "Coconut", "nut", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0000000-0000-0000-0000-000000000009"), "Beverages", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nuwara Eliya", 9, "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=500&auto=format&fit=crop", true, "Tea (BOP)", "kg", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0000000-0000-0000-0000-00000000000a"), "Spices", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Matara", 10, "https://images.unsplash.com/photo-1509358271058-acd22cc93898?w=500&auto=format&fit=crop", true, "Cinnamon (Alba)", "kg", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0000000-0000-0000-0000-00000000000b"), "Vegetables", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nuwara Eliya", 11, "https://images.unsplash.com/photo-1587049352846-4a222e784d38?w=500&auto=format&fit=crop", true, "Leeks", "kg", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0000000-0000-0000-0000-00000000000c"), "Vegetables", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nuwara Eliya", 12, "https://images.unsplash.com/photo-1594282486552-05b4d80fbb9f?w=500&auto=format&fit=crop", true, "Cabbage", "kg", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0000000-0000-0000-0000-00000000000d"), "Vegetables", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kurunegala", 13, "https://images.unsplash.com/photo-1570586437263-ab629fccc818?w=500&auto=format&fit=crop", true, "Pumpkin", "kg", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0000000-0000-0000-0000-00000000000e"), "Vegetables", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Badulla", 14, "https://images.unsplash.com/photo-1567375698348-5d9d5ae10c3a?w=500&auto=format&fit=crop", true, "Beans", "kg", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0000000-0000-0000-0000-00000000000f"), "Fruits", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Gampaha", 15, "https://images.unsplash.com/photo-1517282009859-f000ec3b26fe?w=500&auto=format&fit=crop", true, "Papaya", "kg", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0000000-0000-0000-0000-000000000010"), "Fruits", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Jaffna", 16, "https://images.unsplash.com/photo-1553279768-865429fa0078?w=500&auto=format&fit=crop", true, "Mango", "kg", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0000000-0000-0000-0000-000000000011"), "Spices", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Matale", 17, "https://images.unsplash.com/photo-1599909533601-aa1e5c0fb0a4?w=500&auto=format&fit=crop", true, "Pepper (Black)", "kg", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0000000-0000-0000-0000-000000000012"), "Vegetables", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Jaffna", 18, "https://images.unsplash.com/photo-1615485290382-441e4d049cb5?w=500&auto=format&fit=crop", true, "Drumstick (Murunga)", "kg", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0000000-0000-0000-0000-000000000013"), "Vegetables", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dambulla", 19, "https://images.unsplash.com/photo-1613881553903-4bedfcea4dd1?w=500&auto=format&fit=crop", true, "Brinjal (Eggplant)", "kg", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0000000-0000-0000-0000-000000000014"), "Fruits", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Colombo", 20, "https://images.unsplash.com/photo-1590502593747-42a996133562?w=500&auto=format&fit=crop", true, "Lime", "kg", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "User",
                columns: new[] { "Id", "AvatarUrl", "CreatedAt", "Email", "FullName", "IsActive", "PasswordHash", "Phone", "Region", "Role" },
                values: new object[,]
                {
                    { new Guid("f0000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2024, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "farmer@agriconnect.lk", "Kamal Perera", true, "600000.ezjZQwN7EZYdigiik+HqbA==.iE33PwV1IisZPhE+tOJgA6uVMgcWO0OqPdC6pWkc+9w=", "+94771234567", "Nuwara Eliya", "Farmer" },
                    { new Guid("f0000000-0000-0000-0000-000000000002"), null, new DateTimeOffset(new DateTime(2024, 2, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "farmer2@agriconnect.lk", "Saman Silva", true, "600000.ezjZQwN7EZYdigiik+HqbA==.iE33PwV1IisZPhE+tOJgA6uVMgcWO0OqPdC6pWkc+9w=", "+94779876543", "Kandy", "Farmer" },
                    { new Guid("f0000000-0000-0000-0000-000000000010"), null, new DateTimeOffset(new DateTime(2024, 1, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "buyer@agriconnect.lk", "Nihal Fernando", true, "600000.ezjZQwN7EZYdigiik+HqbA==.iE33PwV1IisZPhE+tOJgA6uVMgcWO0OqPdC6pWkc+9w=", "+94701234567", "Colombo", "Buyer" },
                    { new Guid("f0000000-0000-0000-0000-000000000050"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "officer@agriconnect.lk", "Officer Demo", true, "600000.ezjZQwN7EZYdigiik+HqbA==.iE33PwV1IisZPhE+tOJgA6uVMgcWO0OqPdC6pWkc+9w=", "+94711234567", "Kandy", "Officer" },
                    { new Guid("f0000000-0000-0000-0000-000000000099"), null, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "admin@agriconnect.lk", "N. Perera", true, "600000.ezjZQwN7EZYdigiik+HqbA==.iE33PwV1IisZPhE+tOJgA6uVMgcWO0OqPdC6pWkc+9w=", "+94112345678", "Nuwara Eliya", "Administrator" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListingPhotos_ListingId",
                table: "ListingPhotos",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_CropId_RegionId_Status_ClaimedGrade",
                table: "Listings",
                columns: new[] { "CropId", "RegionId", "Status", "ClaimedGrade" });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_FarmerId",
                table: "Listings",
                column: "FarmerId");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_RegionId",
                table: "Listings",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceSuggestions_ListingId",
                table: "PriceSuggestions",
                column: "ListingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TodayPriceCatalogItems_DisplayOrder",
                table: "TodayPriceCatalogItems",
                column: "DisplayOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListingPhotos");

            migrationBuilder.DropTable(
                name: "PriceSuggestions");

            migrationBuilder.DropTable(
                name: "TodayPriceCatalogItems");

            migrationBuilder.DropTable(
                name: "Listings");

            migrationBuilder.DeleteData(
                table: "Crop",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Crop",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Crop",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Crop",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "Crop",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "Crop",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "Crop",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "Crop",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "Crop",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "Crop",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000000a"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-00000000000a"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-00000000000b"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-00000000000c"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-00000000000d"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-00000000000e"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-00000000000f"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000014"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000015"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000016"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000018"));

            migrationBuilder.DeleteData(
                table: "Region",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000019"));

            migrationBuilder.DeleteData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-0000-0000-000000000050"));

            migrationBuilder.DeleteData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-0000-0000-000000000099"));

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "User");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "User");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "User");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "User");

            migrationBuilder.DropColumn(
                name: "CollectionCentreId",
                table: "Region");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Crop");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "User",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);
        }
    }
}
