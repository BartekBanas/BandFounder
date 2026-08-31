using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BandFounder.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableEmailVerificationDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryAttemptCount",
                table: "EmailVerificationTokens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryLastAttemptAt",
                table: "EmailVerificationTokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryLastError",
                table: "EmailVerificationTokens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryNotBeforeUtc",
                table: "EmailVerificationTokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryProcessedAt",
                table: "EmailVerificationTokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryStatus",
                table: "EmailVerificationTokens",
                type: "text",
                nullable: false,
                defaultValue: "Sent");

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationTokens_DeliveryStatus_DeliveryNotBeforeUtc",
                table: "EmailVerificationTokens",
                columns: new[] { "DeliveryStatus", "DeliveryNotBeforeUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailVerificationTokens_DeliveryStatus_DeliveryNotBeforeUtc",
                table: "EmailVerificationTokens");

            migrationBuilder.DropColumn(
                name: "DeliveryAttemptCount",
                table: "EmailVerificationTokens");

            migrationBuilder.DropColumn(
                name: "DeliveryLastAttemptAt",
                table: "EmailVerificationTokens");

            migrationBuilder.DropColumn(
                name: "DeliveryLastError",
                table: "EmailVerificationTokens");

            migrationBuilder.DropColumn(
                name: "DeliveryNotBeforeUtc",
                table: "EmailVerificationTokens");

            migrationBuilder.DropColumn(
                name: "DeliveryProcessedAt",
                table: "EmailVerificationTokens");

            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                table: "EmailVerificationTokens");
        }
    }
}
