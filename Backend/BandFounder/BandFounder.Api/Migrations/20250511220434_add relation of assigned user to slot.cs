using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BandFounder.Api.Migrations
{
    /// <inheritdoc />
    public partial class addrelationofassignedusertoslot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssigneeId",
                table: "MusicianSlots",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicianSlots_AssigneeId",
                table: "MusicianSlots",
                column: "AssigneeId");

            migrationBuilder.AddForeignKey(
                name: "FK_MusicianSlots_Account_AssigneeId",
                table: "MusicianSlots",
                column: "AssigneeId",
                principalTable: "Account",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MusicianSlots_Account_AssigneeId",
                table: "MusicianSlots");

            migrationBuilder.DropIndex(
                name: "IX_MusicianSlots_AssigneeId",
                table: "MusicianSlots");

            migrationBuilder.DropColumn(
                name: "AssigneeId",
                table: "MusicianSlots");
        }
    }
}
