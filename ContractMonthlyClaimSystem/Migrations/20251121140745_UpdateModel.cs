using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractMonthlyClaimSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkClaims",
                table: "WorkClaims");

            migrationBuilder.RenameTable(
                name: "WorkClaims",
                newName: "LecturerClaims");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LecturerClaims",
                table: "LecturerClaims",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LecturerClaims",
                table: "LecturerClaims");

            migrationBuilder.RenameTable(
                name: "LecturerClaims",
                newName: "WorkClaims");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkClaims",
                table: "WorkClaims",
                column: "Id");
        }
    }
}
