using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myPhotoBiz.Migrations
{
    /// <inheritdoc />
    public partial class AddContractTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create ContractTemplates table
            migrationBuilder.CreateTable(
                name: "ContractTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractTemplates", x => x.Id);
                });

            // Add TemplateId column to Contracts table
            migrationBuilder.AddColumn<int>(
                name: "TemplateId",
                table: "Contracts",
                type: "INTEGER",
                nullable: true);

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_ContractTemplate_Category",
                table: "ContractTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ContractTemplate_IsActive",
                table: "ContractTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_TemplateId",
                table: "Contracts",
                column: "TemplateId");

            // Add foreign key
            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_ContractTemplates_TemplateId",
                table: "Contracts",
                column: "TemplateId",
                principalTable: "ContractTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_ContractTemplates_TemplateId",
                table: "Contracts");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_Contracts_TemplateId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_ContractTemplate_IsActive",
                table: "ContractTemplates");

            migrationBuilder.DropIndex(
                name: "IX_ContractTemplate_Category",
                table: "ContractTemplates");

            // Drop TemplateId column
            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "Contracts");

            // Drop ContractTemplates table
            migrationBuilder.DropTable(
                name: "ContractTemplates");
        }
    }
}
