using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddImportIgnoreRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportIgnoreRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: true),
                    MatchText = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    MatchType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportIgnoreRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportIgnoreRules_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportIgnoreRules_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportIgnoreRules_AccountId_MatchText_MatchType",
                table: "ImportIgnoreRules",
                columns: new[] { "AccountId", "MatchText", "MatchType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportIgnoreRules_CreatedByUserId",
                table: "ImportIgnoreRules",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportIgnoreRules_MatchText_MatchType",
                table: "ImportIgnoreRules",
                columns: new[] { "MatchText", "MatchType" },
                unique: true,
                filter: "\"AccountId\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportIgnoreRules");
        }
    }
}
