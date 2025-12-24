using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _20241129402SoruCevapPortali.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Answers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "Answers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "Answers");
        }
    }
}
