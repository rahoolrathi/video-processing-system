using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace utube.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicUrlToVideo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicUrl",
                table: "Videos",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicUrl",
                table: "Videos");
        }
    }
}
