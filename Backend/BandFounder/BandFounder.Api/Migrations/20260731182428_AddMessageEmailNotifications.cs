using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BandFounder.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageEmailNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountNotificationPreferences",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailOnNewMessage = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EmailUnreadDelayMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 1440)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountNotificationPreferences", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_AccountNotificationPreferences_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO "AccountNotificationPreferences" ("AccountId", "EmailOnNewMessage", "EmailUnreadDelayMinutes")
                SELECT "Id", TRUE, 1440
                FROM "Account"
                ON CONFLICT ("AccountId") DO NOTHING;
                """);

            migrationBuilder.CreateTable(
                name: "EmailNotificationOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    NotBeforeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    UnreadCountHint = table.Column<int>(type: "integer", nullable: false),
                    LatestSenderName = table.Column<string>(type: "text", nullable: false),
                    ChatroomName = table.Column<string>(type: "text", nullable: false),
                    Snippet = table.Column<string>(type: "text", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailNotificationOutbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailNotificationOutbox_Account_RecipientAccountId",
                        column: x => x.RecipientAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailNotificationOutbox_Chatrooms_ChatRoomId",
                        column: x => x.ChatRoomId,
                        principalTable: "Chatrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailNotificationOutbox_ChatRoomId",
                table: "EmailNotificationOutbox",
                column: "ChatRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailNotificationOutbox_RecipientAccountId_ChatRoomId",
                table: "EmailNotificationOutbox",
                columns: new[] { "RecipientAccountId", "ChatRoomId" },
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_EmailNotificationOutbox_Status_NotBeforeUtc",
                table: "EmailNotificationOutbox",
                columns: new[] { "Status", "NotBeforeUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountNotificationPreferences");

            migrationBuilder.DropTable(
                name: "EmailNotificationOutbox");
        }
    }
}
