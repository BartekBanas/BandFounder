using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BandFounder.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountPasswordVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PasswordVersion",
                table: "Account",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordVersion",
                table: "Account");
        }
    }
}
