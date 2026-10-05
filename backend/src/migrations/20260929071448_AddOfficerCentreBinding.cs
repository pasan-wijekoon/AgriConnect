using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.src.migrations
{
    /// <inheritdoc />
    public partial class AddOfficerCentreBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CollectionCentreId",
                table: "User",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-0000-0000-000000000001"),
                column: "CollectionCentreId",
                value: null);

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-0000-0000-000000000002"),
                column: "CollectionCentreId",
                value: null);

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-0000-0000-000000000010"),
                column: "CollectionCentreId",
                value: null);

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-0000-0000-000000000050"),
                column: "CollectionCentreId",
                value: null);

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-0000-0000-000000000099"),
                column: "CollectionCentreId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_User_CollectionCentreId",
                table: "User",
                column: "CollectionCentreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_CollectionCentreId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "CollectionCentreId",
                table: "User");
        }
    }
}
