using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BandFounder.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakePendingEmailNotificationIndexUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailNotificationOutbox_RecipientAccountId_ChatRoomId",
                table: "EmailNotificationOutbox");

            migrationBuilder.CreateIndex(
                name: "IX_EmailNotificationOutbox_RecipientAccountId_ChatRoomId",
                table: "EmailNotificationOutbox",
                columns: new[] { "RecipientAccountId", "ChatRoomId" },
                unique: true,
                filter: "\"Status\" = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailNotificationOutbox_RecipientAccountId_ChatRoomId",
                table: "EmailNotificationOutbox");

            migrationBuilder.CreateIndex(
                name: "IX_EmailNotificationOutbox_RecipientAccountId_ChatRoomId",
                table: "EmailNotificationOutbox",
                columns: new[] { "RecipientAccountId", "ChatRoomId" },
                filter: "\"Status\" = 'Pending'");
        }
    }
}
