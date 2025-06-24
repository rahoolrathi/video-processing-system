using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace utube.Migrations
{
    /// <inheritdoc />
    public partial class add2fieldsinvideotable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptionKey",
                table: "Videos",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyId",
                table: "Videos",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptionKey",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "KeyId",
                table: "Videos");
        }
    }
}
