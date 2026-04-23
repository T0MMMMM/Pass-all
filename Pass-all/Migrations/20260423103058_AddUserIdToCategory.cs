using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Passall.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ProfileCategory",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ProfileCategory_UserId",
                table: "ProfileCategory",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileCategory_User_UserId",
                table: "ProfileCategory",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProfileCategory_User_UserId",
                table: "ProfileCategory");

            migrationBuilder.DropIndex(
                name: "IX_ProfileCategory_UserId",
                table: "ProfileCategory");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ProfileCategory");
        }
    }
}
