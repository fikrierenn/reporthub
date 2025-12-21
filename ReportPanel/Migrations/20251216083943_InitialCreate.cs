using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReportPanel.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataSources",
                columns: table => new
                {
                    DataSourceKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConnString = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSources", x => x.DataSourceKey);
                });

            migrationBuilder.CreateTable(
                name: "ReportCatalog",
                columns: table => new
                {
                    ReportId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DataSourceKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProcName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParamSchemaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllowedRoles = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportCatalog", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK_ReportCatalog_DataSources_DataSourceKey",
                        column: x => x.DataSourceKey,
                        principalTable: "DataSources",
                        principalColumn: "DataSourceKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportRunLog",
                columns: table => new
                {
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    DataSourceKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ParamsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RunAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DurationMs = table.Column<int>(type: "int", nullable: true),
                    ResultRowCount = table.Column<int>(type: "int", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportRunLog", x => x.RunId);
                    table.ForeignKey(
                        name: "FK_ReportRunLog_ReportCatalog_ReportId",
                        column: x => x.ReportId,
                        principalTable: "ReportCatalog",
                        principalColumn: "ReportId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportCatalog_DataSourceKey",
                table: "ReportCatalog",
                column: "DataSourceKey");

            migrationBuilder.CreateIndex(
                name: "IX_ReportRunLog_ReportId",
                table: "ReportRunLog",
                column: "ReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportRunLog");

            migrationBuilder.DropTable(
                name: "ReportCatalog");

            migrationBuilder.DropTable(
                name: "DataSources");
        }
    }
}
