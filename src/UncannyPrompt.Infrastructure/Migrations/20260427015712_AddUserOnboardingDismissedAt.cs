using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UncannyPrompt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOnboardingDismissedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OnboardingDismissedAt",
                table: "Users",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnboardingDismissedAt",
                table: "Users");
        }
    }
}
