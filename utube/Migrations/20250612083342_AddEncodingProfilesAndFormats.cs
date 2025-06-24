using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace utube.Migrations
{
    /// <inheritdoc />
    public partial class AddEncodingProfilesAndFormats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EncodingProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Resolutions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BitratesKbps = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncodingProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Formats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EncodingProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormatType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Formats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Formats_EncodingProfiles_EncodingProfileId",
                        column: x => x.EncodingProfileId,
                        principalTable: "EncodingProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Formats_EncodingProfileId",
                table: "Formats",
                column: "EncodingProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Formats");

            migrationBuilder.DropTable(
                name: "EncodingProfiles");
        }
    }
}
