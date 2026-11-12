using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHelpDeskManagement.Migrations
{
    /// <inheritdoc />
    public partial class addclass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByEmail",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByEmail",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Tickets");
        }
    }
}
