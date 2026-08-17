using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LotteryAnalytics.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionMatchGranularities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ExactMatch",
                table: "PredictionRecords",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Last2Match",
                table: "PredictionRecords",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Last3Match",
                table: "PredictionRecords",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExactMatch",
                table: "PredictionRecords");

            migrationBuilder.DropColumn(
                name: "Last2Match",
                table: "PredictionRecords");

            migrationBuilder.DropColumn(
                name: "Last3Match",
                table: "PredictionRecords");
        }
    }
}
