using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.src.migrations
{
    /// <inheritdoc />
    public partial class SetDemoAdminPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-0000-0000-000000000099"),
                column: "PasswordHash",
                value: "600000.EZ+jN5qodC33/GPd+dSJvg==.wsxn846JB/pixYU9C5V9hAkGzMv0yU/e8o/CYZpIsX4=");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-0000-0000-000000000099"),
                column: "PasswordHash",
                value: "600000.ezjZQwN7EZYdigiik+HqbA==.iE33PwV1IisZPhE+tOJgA6uVMgcWO0OqPdC6pWkc+9w=");
        }
    }
}
