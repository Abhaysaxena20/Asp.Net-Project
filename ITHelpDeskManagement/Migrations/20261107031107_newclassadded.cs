using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHelpDeskManagement.Migrations
{
    /// <inheritdoc />
    public partial class newclassadded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommentHiddenForUsers_ticketcomment_CommentId",
                table: "CommentHiddenForUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_CommentHiddenForUsers_ticketcomment_CommentId",
                table: "CommentHiddenForUsers",
                column: "CommentId",
                principalTable: "ticketcomment",
                principalColumn: "CommentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommentHiddenForUsers_ticketcomment_CommentId",
                table: "CommentHiddenForUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_CommentHiddenForUsers_ticketcomment_CommentId",
                table: "CommentHiddenForUsers",
                column: "CommentId",
                principalTable: "ticketcomment",
                principalColumn: "CommentId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
