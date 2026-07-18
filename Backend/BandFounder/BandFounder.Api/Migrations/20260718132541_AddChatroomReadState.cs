using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BandFounder.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChatroomReadState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatroomReadStates",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatroomReadStates", x => new { x.AccountId, x.ChatRoomId });
                    table.ForeignKey(
                        name: "FK_ChatroomReadStates_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatroomReadStates_Chatrooms_ChatRoomId",
                        column: x => x.ChatRoomId,
                        principalTable: "Chatrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatroomReadStates_ChatRoomId",
                table: "ChatroomReadStates",
                column: "ChatRoomId");

            migrationBuilder.Sql("""
                INSERT INTO "ChatroomReadStates" ("AccountId", "ChatRoomId", "LastReadAt")
                SELECT "MembersId", "ChatroomsId", NOW() AT TIME ZONE 'utc'
                FROM "AccountChatroom"
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatroomReadStates");
        }
    }
}
