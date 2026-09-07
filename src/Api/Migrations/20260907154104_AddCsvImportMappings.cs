using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCsvImportMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CsvImportMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    DateColumn = table.Column<string>(type: "TEXT", nullable: false),
                    DescriptionColumn = table.Column<string>(type: "TEXT", nullable: false),
                    AmountColumn = table.Column<string>(type: "TEXT", nullable: true),
                    DebitColumn = table.Column<string>(type: "TEXT", nullable: true),
                    CreditColumn = table.Column<string>(type: "TEXT", nullable: true),
                    DateFormat = table.Column<string>(type: "TEXT", nullable: false),
                    Delimiter = table.Column<string>(type: "TEXT", nullable: false),
                    HasHeaderRow = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CsvImportMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CsvImportMappings_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CsvImportMappings_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CsvImportMappings_AccountId",
                table: "CsvImportMappings",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CsvImportMappings_CreatedByUserId",
                table: "CsvImportMappings",
                column: "CreatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CsvImportMappings");
        }
    }
}
