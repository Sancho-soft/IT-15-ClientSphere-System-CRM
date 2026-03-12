using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientSphere.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignActualRevenue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ActualRevenue",
                table: "Campaigns",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualRevenue",
                table: "Campaigns");
        }
    }
}
