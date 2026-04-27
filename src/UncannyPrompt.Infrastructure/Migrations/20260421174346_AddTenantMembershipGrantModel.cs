using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UncannyPrompt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantMembershipGrantModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 3,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "CanGrant",
                table: "TenantMemberships",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "GrantedAt",
                table: "TenantMemberships",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GrantedByUserId",
                table: "TenantMemberships",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_GrantedByUserId",
                table: "TenantMemberships",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_TenantId_GrantedByUserId",
                table: "TenantMemberships",
                columns: new[] { "TenantId", "GrantedByUserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_TenantMemberships_Users_GrantedByUserId",
                table: "TenantMemberships",
                column: "GrantedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantMemberships_Users_GrantedByUserId",
                table: "TenantMemberships");

            migrationBuilder.DropIndex(
                name: "IX_TenantMemberships_GrantedByUserId",
                table: "TenantMemberships");

            migrationBuilder.DropIndex(
                name: "IX_TenantMemberships_TenantId_GrantedByUserId",
                table: "TenantMemberships");

            migrationBuilder.DropColumn(
                name: "CanGrant",
                table: "TenantMemberships");

            migrationBuilder.DropColumn(
                name: "GrantedAt",
                table: "TenantMemberships");

            migrationBuilder.DropColumn(
                name: "GrantedByUserId",
                table: "TenantMemberships");

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 3);
        }
    }
}
