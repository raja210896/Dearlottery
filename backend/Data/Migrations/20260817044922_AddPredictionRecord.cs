using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LotteryAnalytics.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PredictionRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DrawDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DrawTime = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DigitLength = table.Column<int>(type: "int", nullable: false),
                    Candidates = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActualResult = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsEvaluated = table.Column<bool>(type: "bit", nullable: false),
                    MatchFound = table.Column<bool>(type: "bit", nullable: true),
                    MatchPosition = table.Column<int>(type: "int", nullable: true),
                    EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LotteryResultId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PredictionRecords_LotteryResults_LotteryResultId",
                        column: x => x.LotteryResultId,
                        principalTable: "LotteryResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PredictionRecords_DrawDate",
                table: "PredictionRecords",
                column: "DrawDate");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionRecords_DrawDate_DrawTime_DigitLength_ModelVersion",
                table: "PredictionRecords",
                columns: new[] { "DrawDate", "DrawTime", "DigitLength", "ModelVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PredictionRecords_DrawTime",
                table: "PredictionRecords",
                column: "DrawTime");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionRecords_LotteryResultId",
                table: "PredictionRecords",
                column: "LotteryResultId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PredictionRecords");
        }
    }
}
