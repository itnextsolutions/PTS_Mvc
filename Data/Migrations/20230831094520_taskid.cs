using Microsoft.EntityFrameworkCore.Migrations;

namespace MVC_BugTracker.Data.Migrations
{
    public partial class taskid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TicketId",
                table: "TicketTask",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TicketTask_TicketId",
                table: "TicketTask",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketTask_Ticket_TicketId",
                table: "TicketTask",
                column: "TicketId",
                principalTable: "Ticket",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketTask_Ticket_TicketId",
                table: "TicketTask");

            migrationBuilder.DropIndex(
                name: "IX_TicketTask_TicketId",
                table: "TicketTask");

            migrationBuilder.DropColumn(
                name: "TicketId",
                table: "TicketTask");
        }
    }
}
