using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCIS.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToClub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "JoinedAt",
                table: "Memberships",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedAt",
                table: "Memberships",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Events",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Events",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Clubs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "ClubId",
                table: "Announcements",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_ClubId",
                table: "Memberships",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_UserId",
                table: "Memberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_ClubId",
                table: "Events",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_CreatedByAdminId",
                table: "Clubs",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_PresidentId",
                table: "Clubs",
                column: "PresidentId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_ClubId",
                table: "Announcements",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_CreatedByUserId",
                table: "Announcements",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Announcements_AspNetUsers_CreatedByUserId",
                table: "Announcements",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Announcements_Clubs_ClubId",
                table: "Announcements",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "ClubId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_AspNetUsers_CreatedByAdminId",
                table: "Clubs",
                column: "CreatedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_AspNetUsers_PresidentId",
                table: "Clubs",
                column: "PresidentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Clubs_ClubId",
                table: "Events",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "ClubId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Memberships_AspNetUsers_UserId",
                table: "Memberships",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Memberships_Clubs_ClubId",
                table: "Memberships",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "ClubId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Announcements_AspNetUsers_CreatedByUserId",
                table: "Announcements");

            migrationBuilder.DropForeignKey(
                name: "FK_Announcements_Clubs_ClubId",
                table: "Announcements");

            migrationBuilder.DropForeignKey(
                name: "FK_Clubs_AspNetUsers_CreatedByAdminId",
                table: "Clubs");

            migrationBuilder.DropForeignKey(
                name: "FK_Clubs_AspNetUsers_PresidentId",
                table: "Clubs");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_Clubs_ClubId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Memberships_AspNetUsers_UserId",
                table: "Memberships");

            migrationBuilder.DropForeignKey(
                name: "FK_Memberships_Clubs_ClubId",
                table: "Memberships");

            migrationBuilder.DropIndex(
                name: "IX_Memberships_ClubId",
                table: "Memberships");

            migrationBuilder.DropIndex(
                name: "IX_Memberships_UserId",
                table: "Memberships");

            migrationBuilder.DropIndex(
                name: "IX_Events_ClubId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_CreatedByAdminId",
                table: "Clubs");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_PresidentId",
                table: "Clubs");

            migrationBuilder.DropIndex(
                name: "IX_Announcements_ClubId",
                table: "Announcements");

            migrationBuilder.DropIndex(
                name: "IX_Announcements_CreatedByUserId",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "JoinedAt",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "RequestedAt",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Clubs");

            migrationBuilder.AlterColumn<int>(
                name: "ClubId",
                table: "Announcements",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
