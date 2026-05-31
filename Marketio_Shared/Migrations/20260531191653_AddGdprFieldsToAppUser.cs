using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketio_Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddGdprFieldsToAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentGivenDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MarketingOptIn",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrivacyConsentGiven",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TermsConsentGiven",
                table: "AspNetUsers",
                type: "bit",
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
