using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddImportAssignmentRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportAssignmentRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: true),
                    MatchText = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    MatchType = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    NeedWant = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportAssignmentRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportAssignmentRules_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportAssignmentRules_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportAssignmentRules_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportAssignmentRuleTags",
                columns: table => new
                {
                    RuleId = table.Column<int>(type: "INTEGER", nullable: false),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportAssignmentRuleTags", x => new { x.RuleId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ImportAssignmentRuleTags_ImportAssignmentRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "ImportAssignmentRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportAssignmentRuleTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportAssignmentRules_AccountId_MatchText_MatchType",
                table: "ImportAssignmentRules",
                columns: new[] { "AccountId", "MatchText", "MatchType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportAssignmentRules_CategoryId",
                table: "ImportAssignmentRules",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportAssignmentRules_CreatedByUserId",
                table: "ImportAssignmentRules",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportAssignmentRules_MatchText_MatchType",
                table: "ImportAssignmentRules",
                columns: new[] { "MatchText", "MatchType" },
                unique: true,
                filter: "\"AccountId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ImportAssignmentRuleTags_TagId",
                table: "ImportAssignmentRuleTags",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportAssignmentRuleTags");

            migrationBuilder.DropTable(
                name: "ImportAssignmentRules");
        }
    }
}
