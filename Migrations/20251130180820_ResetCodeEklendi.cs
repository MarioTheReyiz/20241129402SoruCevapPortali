using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _20241129402SoruCevapPortali.Migrations
{
    /// <inheritdoc />
    public partial class ResetCodeEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResetCode",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResetCode",
                table: "Users");
        }
    }
}
