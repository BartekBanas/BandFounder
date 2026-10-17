using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BandFounder.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddArtistSourcesAndLinkedPlaylists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountArtistSource",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountsId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtistsId = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    LinkedPlaylistId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountArtistSource", x => x.Id);
                    table.CheckConstraint("CK_AccountArtistSource_LinkedPlaylistId", "(\"Kind\" <> 'LinkedPlaylist' AND \"LinkedPlaylistId\" IS NULL) OR (\"Kind\" = 'LinkedPlaylist' AND \"LinkedPlaylistId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_AccountArtistSource_AccountArtist_AccountsId_ArtistsId",
                        columns: x => new { x.AccountsId, x.ArtistsId },
                        principalTable: "AccountArtist",
                        principalColumns: new[] { "AccountsId", "ArtistsId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LinkedPlaylist",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpotifyId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkedPlaylist", x => new { x.AccountId, x.SpotifyId });
                    table.ForeignKey(
                        name: "FK_LinkedPlaylist_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountArtistSource_AccountsId_ArtistsId_Kind",
                table: "AccountArtistSource",
                columns: new[] { "AccountsId", "ArtistsId", "Kind" },
                unique: true,
                filter: "\"LinkedPlaylistId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountArtistSource_AccountsId_ArtistsId_LinkedPlaylistId",
                table: "AccountArtistSource",
                columns: new[] { "AccountsId", "ArtistsId", "LinkedPlaylistId" },
                unique: true,
                filter: "\"LinkedPlaylistId\" IS NOT NULL");

            migrationBuilder.Sql("""
                INSERT INTO "AccountArtistSource" ("Id", "AccountsId", "ArtistsId", "Kind", "LinkedPlaylistId")
                SELECT gen_random_uuid(), "AccountsId", "ArtistsId", 'Manual', NULL
                FROM "AccountArtist";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountArtistSource");

            migrationBuilder.DropTable(
                name: "LinkedPlaylist");
        }
    }
}
