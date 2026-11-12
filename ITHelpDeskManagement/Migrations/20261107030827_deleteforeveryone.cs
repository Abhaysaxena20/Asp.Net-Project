using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHelpDeskManagement.Migrations
{
    /// <inheritdoc />
    public partial class deleteforeveryone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommentHiddenForUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentHiddenForUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentHiddenForUsers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
    name: "FK_CommentHiddenForUsers_ticketcomment_CommentId",
    column: x => x.CommentId,
    principalTable: "ticketcomment",
    principalColumn: "CommentId",
    onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommentHiddenForUsers_CommentId",
                table: "CommentHiddenForUsers",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentHiddenForUsers_UserId",
                table: "CommentHiddenForUsers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommentHiddenForUsers");
        }
    }
}
