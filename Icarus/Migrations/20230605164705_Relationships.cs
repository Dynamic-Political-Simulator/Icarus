using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icarus.Migrations
{
    /// <inheritdoc />
    public partial class Relationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Relationships_Values_OriginId",
                table: "Relationships");

            migrationBuilder.DropForeignKey(
                name: "FK_Relationships_Values_TargetId",
                table: "Relationships");

            migrationBuilder.DropIndex(
                name: "IX_Relationships_OriginId",
                table: "Relationships");

            migrationBuilder.DropIndex(
                name: "IX_Relationships_TargetId",
                table: "Relationships");

            migrationBuilder.DropColumn(
                name: "OriginId",
                table: "Relationships");

            migrationBuilder.DropColumn(
                name: "TargetId",
                table: "Relationships");

            migrationBuilder.AddColumn<string>(
                name: "OriginTag",
                table: "Relationships",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetTag",
                table: "Relationships",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginTag",
                table: "Relationships");

            migrationBuilder.DropColumn(
                name: "TargetTag",
                table: "Relationships");

            migrationBuilder.AddColumn<int>(
                name: "OriginId",
                table: "Relationships",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TargetId",
                table: "Relationships",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_OriginId",
                table: "Relationships",
                column: "OriginId");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_TargetId",
                table: "Relationships",
                column: "TargetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Relationships_Values_OriginId",
                table: "Relationships",
                column: "OriginId",
                principalTable: "Values",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Relationships_Values_TargetId",
                table: "Relationships",
                column: "TargetId",
                principalTable: "Values",
                principalColumn: "Id");
        }
    }
}
