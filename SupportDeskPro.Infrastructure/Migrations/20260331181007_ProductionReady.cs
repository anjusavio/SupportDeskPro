using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportDeskPro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductionReady : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketAssignmentHistories_Users_AssignedById",
                table: "TicketAssignmentHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketStatusHistories_Users_ChangedById",
                table: "TicketStatusHistories");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AISuggestedCategoryId",
                table: "Tickets",
                column: "AISuggestedCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketAssignmentHistories_Users_AssignedById",
                table: "TicketAssignmentHistories",
                column: "AssignedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketNumberSequences_Tenants_TenantId",
                table: "TicketNumberSequences",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Categories_AISuggestedCategoryId",
                table: "Tickets",
                column: "AISuggestedCategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketStatusHistories_Users_ChangedById",
                table: "TicketStatusHistories",
                column: "ChangedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketAssignmentHistories_Users_AssignedById",
                table: "TicketAssignmentHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketNumberSequences_Tenants_TenantId",
                table: "TicketNumberSequences");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Categories_AISuggestedCategoryId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketStatusHistories_Users_ChangedById",
                table: "TicketStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_AISuggestedCategoryId",
                table: "Tickets");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketAssignmentHistories_Users_AssignedById",
                table: "TicketAssignmentHistories",
                column: "AssignedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketStatusHistories_Users_ChangedById",
                table: "TicketStatusHistories",
                column: "ChangedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
