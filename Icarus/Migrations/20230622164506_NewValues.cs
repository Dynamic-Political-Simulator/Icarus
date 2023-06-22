using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icarus.Migrations
{
    /// <inheritdoc />
    public partial class NewValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Tokens",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "TokenType",
                table: "Tokens");

            migrationBuilder.AddColumn<string>(
                name: "TokenTypeId",
                table: "Tokens",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "WealthMod",
                table: "Modifiers",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "WealthMod",
                table: "Goods",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tokens",
                table: "Tokens",
                column: "PlayerCharacterId");

            migrationBuilder.CreateTable(
                name: "TokenTypes",
                columns: table => new
                {
                    TokenTypeName = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenTypes", x => x.TokenTypeName);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_TokenTypeId",
                table: "Tokens",
                column: "TokenTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tokens_TokenTypes_TokenTypeId",
                table: "Tokens",
                column: "TokenTypeId",
                principalTable: "TokenTypes",
                principalColumn: "TokenTypeName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tokens_TokenTypes_TokenTypeId",
                table: "Tokens");

            migrationBuilder.DropTable(
                name: "TokenTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tokens",
                table: "Tokens");

            migrationBuilder.DropIndex(
                name: "IX_Tokens_TokenTypeId",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "TokenTypeId",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "WealthMod",
                table: "Modifiers");

            migrationBuilder.DropColumn(
                name: "WealthMod",
                table: "Goods");

            migrationBuilder.AddColumn<int>(
                name: "TokenType",
                table: "Tokens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tokens",
                table: "Tokens",
                columns: new[] { "PlayerCharacterId", "TokenType" });
        }
    }
}
