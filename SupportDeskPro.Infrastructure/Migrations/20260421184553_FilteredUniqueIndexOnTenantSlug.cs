using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportDeskPro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FilteredUniqueIndexOnTenantSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old unique index
            migrationBuilder.DropIndex(
                name: "UX_Tenants_Slug",
                table: "Tenants");

            // Create filtered unique index — excludes soft deleted 
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX UX_Tenants_Slug ON Tenants(Slug) WHERE IsDeleted = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
               name: "UX_Tenants_Slug",
               table: "Tenants");

            migrationBuilder.CreateIndex(
                name: "UX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);
        }
    }
}
