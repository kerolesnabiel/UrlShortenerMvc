using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortenerMvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClicksTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clicks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Referrer = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Country = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clicks_Links_LinkId",
                        column: x => x.LinkId,
                        principalTable: "Links",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clicks_LinkId_Timestamp",
                table: "Clicks",
                columns: new[] { "LinkId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clicks");
        }
    }
}
