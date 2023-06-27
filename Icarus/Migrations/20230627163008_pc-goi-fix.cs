using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icarus.Migrations
{
    /// <inheritdoc />
    public partial class pcgoifix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_GroupOfInterests_GroupOfInterestId",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_GroupOfInterestId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "GroupOfInterestId",
                table: "Characters");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_GoIid",
                table: "Characters",
                column: "GoIid");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_GroupOfInterests_GoIid",
                table: "Characters",
                column: "GoIid",
                principalTable: "GroupOfInterests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_GroupOfInterests_GoIid",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_GoIid",
                table: "Characters");

            migrationBuilder.AddColumn<int>(
                name: "GroupOfInterestId",
                table: "Characters",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Characters_GroupOfInterestId",
                table: "Characters",
                column: "GroupOfInterestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_GroupOfInterests_GroupOfInterestId",
                table: "Characters",
                column: "GroupOfInterestId",
                principalTable: "GroupOfInterests",
                principalColumn: "Id");
        }
    }
}
