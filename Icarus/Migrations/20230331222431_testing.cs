using Microsoft.EntityFrameworkCore.Migrations;

namespace Icarus.Migrations
{
    public partial class testing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Nations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Gamestates",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NationId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gamestates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Gamestates_Nations_NationId",
                        column: x => x.NationId,
                        principalTable: "Nations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Provinces",
                columns: table => new
                {
                    ProvinceId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    NationId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Provinces", x => x.ProvinceId);
                    table.ForeignKey(
                        name: "FK_Provinces_Nations_NationId",
                        column: x => x.NationId,
                        principalTable: "Nations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Modifiers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    Duration = table.Column<int>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    NationId = table.Column<int>(nullable: true),
                    ProvinceId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Modifiers_Nations_NationId",
                        column: x => x.NationId,
                        principalTable: "Nations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Modifiers_Provinces_ProvinceId",
                        column: x => x.ProvinceId,
                        principalTable: "Provinces",
                        principalColumn: "ProvinceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Values",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true),
                    _Value = table.Column<float>(nullable: false),
                    RelationInducedChange = table.Column<float>(nullable: false),
                    ProvinceId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Values", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Values_Provinces_ProvinceId",
                        column: x => x.ProvinceId,
                        principalTable: "Provinces",
                        principalColumn: "ProvinceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValueModifiers",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Modifier = table.Column<float>(nullable: false),
                    Decay = table.Column<float>(nullable: false),
                    ValueName = table.Column<string>(nullable: true),
                    ModifierWrapperId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValueModifiers", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ValueModifiers_Modifiers_ModifierWrapperId",
                        column: x => x.ModifierWrapperId,
                        principalTable: "Modifiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Relationships",
                columns: table => new
                {
                    ValueRelationShipId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginId1 = table.Column<int>(nullable: true),
                    OriginId = table.Column<string>(nullable: true),
                    TargetId1 = table.Column<int>(nullable: true),
                    TargetId = table.Column<string>(nullable: true),
                    Factor = table.Column<float>(nullable: false),
                    Max = table.Column<float>(nullable: false),
                    Min = table.Column<float>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relationships", x => x.ValueRelationShipId);
                    table.ForeignKey(
                        name: "FK_Relationships_Values_OriginId1",
                        column: x => x.OriginId1,
                        principalTable: "Values",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Relationships_Values_TargetId1",
                        column: x => x.TargetId1,
                        principalTable: "Values",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Gamestates_NationId",
                table: "Gamestates",
                column: "NationId");

            migrationBuilder.CreateIndex(
                name: "IX_Modifiers_NationId",
                table: "Modifiers",
                column: "NationId");

            migrationBuilder.CreateIndex(
                name: "IX_Modifiers_ProvinceId",
                table: "Modifiers",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_Provinces_NationId",
                table: "Provinces",
                column: "NationId");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_OriginId1",
                table: "Relationships",
                column: "OriginId1");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_TargetId1",
                table: "Relationships",
                column: "TargetId1");

            migrationBuilder.CreateIndex(
                name: "IX_ValueModifiers_ModifierWrapperId",
                table: "ValueModifiers",
                column: "ModifierWrapperId");

            migrationBuilder.CreateIndex(
                name: "IX_Values_ProvinceId",
                table: "Values",
                column: "ProvinceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Gamestates");

            migrationBuilder.DropTable(
                name: "Relationships");

            migrationBuilder.DropTable(
                name: "ValueModifiers");

            migrationBuilder.DropTable(
                name: "Values");

            migrationBuilder.DropTable(
                name: "Modifiers");

            migrationBuilder.DropTable(
                name: "Provinces");

            migrationBuilder.DropTable(
                name: "Nations");
        }
    }
}
