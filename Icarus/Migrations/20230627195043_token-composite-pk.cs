using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icarus.Migrations
{
    /// <inheritdoc />
    public partial class tokencompositepk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tokens_TokenTypes_TokenTypeId",
                table: "Tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tokens",
                table: "Tokens");

            migrationBuilder.AlterColumn<string>(
                name: "TokenTypeId",
                table: "Tokens",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tokens",
                table: "Tokens",
                columns: new[] { "PlayerCharacterId", "TokenTypeId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Tokens_TokenTypes_TokenTypeId",
                table: "Tokens",
                column: "TokenTypeId",
                principalTable: "TokenTypes",
                principalColumn: "TokenTypeName",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tokens_TokenTypes_TokenTypeId",
                table: "Tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tokens",
                table: "Tokens");

            migrationBuilder.AlterColumn<string>(
                name: "TokenTypeId",
                table: "Tokens",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tokens",
                table: "Tokens",
                column: "PlayerCharacterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tokens_TokenTypes_TokenTypeId",
                table: "Tokens",
                column: "TokenTypeId",
                principalTable: "TokenTypes",
                principalColumn: "TokenTypeName");
        }
    }
}
