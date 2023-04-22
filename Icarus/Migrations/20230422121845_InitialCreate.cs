using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icarus.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Nations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    DiscordId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CanUseAdminCommands = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.DiscordId);
                });

            migrationBuilder.CreateTable(
                name: "GameStates",
                columns: table => new
                {
                    GameStateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TickInterval = table.Column<long>(type: "bigint", nullable: false),
                    LastTickEpoch = table.Column<long>(type: "bigint", nullable: false),
                    NationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameStates", x => x.GameStateId);
                    table.ForeignKey(
                        name: "FK_GameStates_Nations_NationId",
                        column: x => x.NationId,
                        principalTable: "Nations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Provinces",
                columns: table => new
                {
                    ProvinceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NationId = table.Column<int>(type: "int", nullable: false)
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
                name: "Characters",
                columns: table => new
                {
                    CharacterId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CharacterName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiscordUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    YearOfBirth = table.Column<int>(type: "int", nullable: false),
                    YearOfDeath = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.CharacterId);
                    table.ForeignKey(
                        name: "FK_Characters_Users_DiscordUserId",
                        column: x => x.DiscordUserId,
                        principalTable: "Users",
                        principalColumn: "DiscordId");
                });

            migrationBuilder.CreateTable(
                name: "Modifiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    NationId = table.Column<int>(type: "int", nullable: true),
                    ProvinceId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Modifiers_Nations_NationId",
                        column: x => x.NationId,
                        principalTable: "Nations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Modifiers_Provinces_ProvinceId",
                        column: x => x.ProvinceId,
                        principalTable: "Provinces",
                        principalColumn: "ProvinceId");
                });

            migrationBuilder.CreateTable(
                name: "Values",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    _Value = table.Column<float>(type: "real", nullable: false),
                    RelationInducedChange = table.Column<float>(type: "real", nullable: false),
                    ProvinceId = table.Column<int>(type: "int", nullable: false)
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
                name: "Tokens",
                columns: table => new
                {
                    PlayerCharacterId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TokenType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => new { x.PlayerCharacterId, x.TokenType });
                    table.ForeignKey(
                        name: "FK_Tokens_Characters_PlayerCharacterId",
                        column: x => x.PlayerCharacterId,
                        principalTable: "Characters",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValueModifiers",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Modifier = table.Column<float>(type: "real", nullable: false),
                    Decay = table.Column<float>(type: "real", nullable: false),
                    ValueName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifierWrapperId = table.Column<int>(type: "int", nullable: false)
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
                    ValueRelationShipId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginId1 = table.Column<int>(type: "int", nullable: true),
                    OriginId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetId1 = table.Column<int>(type: "int", nullable: true),
                    TargetId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Factor = table.Column<float>(type: "real", nullable: false),
                    Max = table.Column<float>(type: "real", nullable: false),
                    Min = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relationships", x => x.ValueRelationShipId);
                    table.ForeignKey(
                        name: "FK_Relationships_Values_OriginId1",
                        column: x => x.OriginId1,
                        principalTable: "Values",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Relationships_Values_TargetId1",
                        column: x => x.TargetId1,
                        principalTable: "Values",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "GameStates",
                columns: new[] { "GameStateId", "LastTickEpoch", "NationId", "TickInterval" },
                values: new object[] { 1, 0L, null, 3600000L });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_DiscordUserId",
                table: "Characters",
                column: "DiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameStates_NationId",
                table: "GameStates",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameStates");

            migrationBuilder.DropTable(
                name: "Relationships");

            migrationBuilder.DropTable(
                name: "Tokens");

            migrationBuilder.DropTable(
                name: "ValueModifiers");

            migrationBuilder.DropTable(
                name: "Values");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Modifiers");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Provinces");

            migrationBuilder.DropTable(
                name: "Nations");
        }
    }
}
