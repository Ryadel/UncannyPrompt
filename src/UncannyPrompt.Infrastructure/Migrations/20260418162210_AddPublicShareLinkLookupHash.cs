using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UncannyPrompt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicShareLinkLookupHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TokenLookupHash",
                table: "PublicShareLinks",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE PublicShareLinks
                SET TokenLookupHash = TokenHash
                WHERE TokenLookupHash = ''
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PublicShareLinks_TokenLookupHash",
                table: "PublicShareLinks",
                column: "TokenLookupHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PublicShareLinks_TokenLookupHash",
                table: "PublicShareLinks");

            migrationBuilder.DropColumn(
                name: "TokenLookupHash",
                table: "PublicShareLinks");
        }
    }
}
