using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketio_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddGDPRFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentGivenDate",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionRequestedDate",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeletionRequested",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MarketingOptIn",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrivacyConsentGiven",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TermsConsentGiven",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsentGivenDate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DeletionRequestedDate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsDeletionRequested",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MarketingOptIn",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PrivacyConsentGiven",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TermsConsentGiven",
                table: "AspNetUsers");
        }
    }
}
