using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinchpinBackend.Data.Migrations
{
    /// <summary>
    /// cleaning_reports deixa de ser um documento JSON e passa a ter uma coluna por campo do relatório
    /// (simulador, código, categoria, data, achados, descrição, intervenção e usuário).
    /// Os relatórios antigos, que vinham agrupados em "OS" (osCode/osTag + lista "simulator"),
    /// são convertidos em um registro por limpeza.
    /// </summary>
    public partial class RestructureCleaningReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "cleaning_reports", newName: "cleaning_reports_legacy");

            migrationBuilder.CreateTable(
                name: "cleaning_reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SimulatorId = table.Column<int>(type: "int", nullable: true),
                    SimulatorName = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SimulatorCode = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SimulatorCategory = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CleaningCategory = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CleaningDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Findings = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HasDescription = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    HasIntervention = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Intervention = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cleaning_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cleaning_reports_simulators_SimulatorId",
                        column: x => x.SimulatorId,
                        principalTable: "simulators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_cleaning_reports_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(name: "IX_cleaning_reports_CleaningDate", table: "cleaning_reports", column: "CleaningDate");
            migrationBuilder.CreateIndex(name: "IX_cleaning_reports_SimulatorCategory", table: "cleaning_reports", column: "SimulatorCategory");
            migrationBuilder.CreateIndex(name: "IX_cleaning_reports_SimulatorCode", table: "cleaning_reports", column: "SimulatorCode");
            migrationBuilder.CreateIndex(name: "IX_cleaning_reports_SimulatorId", table: "cleaning_reports", column: "SimulatorId");
            migrationBuilder.CreateIndex(name: "IX_cleaning_reports_UserId", table: "cleaning_reports", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_cleaning_reports_UserName", table: "cleaning_reports", column: "UserName");

            // Uma linha por limpeza: itens de "simulator", itens de OS aninhadas e documentos que já eram planos.
            migrationBuilder.Sql(@"
                INSERT INTO cleaning_reports (SimulatorId, SimulatorName, SimulatorCode, SimulatorCategory, CleaningCategory,
                  CleaningDate, Findings, HasDescription, HasIntervention, Intervention, UserId, UserName, CreatedAt, UpdatedAt)
                SELECT
                  (SELECT s.Id FROM simulators s WHERE s.Name = c.SimulatorName LIMIT 1) AS SimulatorId,
                  c.SimulatorName, c.SimulatorCode,
                  COALESCE((SELECT s.Category FROM simulators s WHERE s.Name = c.SimulatorName LIMIT 1), c.SimulatorCategory, 'alta') AS SimulatorCategory,
                  COALESCE(c.CleaningCategory, 'primária') AS CleaningCategory,
                  STR_TO_DATE(c.CleaningDate, '%d-%m-%Y') AS CleaningDate,
                  COALESCE(c.Findings, '') AS Findings,
                  COALESCE(c.HasDescription = 'true', 0) AS HasDescription,
                  COALESCE(c.HasIntervention = 'true', 0) AS HasIntervention,
                  c.Intervention,
                  (SELECT u.Id FROM users u WHERE u.Name = c.UserName LIMIT 1) AS UserId,
                  COALESCE(c.UserName, '') AS UserName,
                  c.CreatedAt, c.UpdatedAt
                FROM (
                  SELECT l.CreatedAt, l.UpdatedAt,
                    COALESCE(j.n_simulatorName, j.simulatorName) AS SimulatorName,
                    COALESCE(j.n_simulatorCode, j.simulatorCode) AS SimulatorCode,
                    COALESCE(j.n_simulatorCategory, j.simulatorCategory) AS SimulatorCategory,
                    COALESCE(j.n_cleaningCategory, j.cleaningCategory) AS CleaningCategory,
                    COALESCE(j.n_date, j.date) AS CleaningDate,
                    COALESCE(j.n_findings, j.findings) AS Findings,
                    COALESCE(j.n_hasDescription, j.hasDescription) AS HasDescription,
                    COALESCE(j.n_hasIntervention, j.hasIntervention) AS HasIntervention,
                    COALESCE(j.n_intervention, j.intervention) AS Intervention,
                    COALESCE(j.n_userName, j.userName) AS UserName
                  FROM cleaning_reports_legacy l,
                  JSON_TABLE(l.Data, '$.simulator[*]' COLUMNS (
                    simulatorName varchar(120) PATH '$.simulatorName',
                    simulatorCode varchar(40) PATH '$.simulatorCode',
                    simulatorCategory varchar(10) PATH '$.simulatorCategory',
                    cleaningCategory varchar(20) PATH '$.cleaningCategory',
                    date varchar(10) PATH '$.date',
                    findings text PATH '$.findings',
                    hasDescription varchar(5) PATH '$.hasDescription',
                    hasIntervention varchar(5) PATH '$.hasIntervention',
                    intervention json PATH '$.intervention',
                    userName varchar(120) PATH '$.user.name',
                    NESTED PATH '$.simulator[*]' COLUMNS (
                      n_simulatorName varchar(120) PATH '$.simulatorName',
                      n_simulatorCode varchar(40) PATH '$.simulatorCode',
                      n_simulatorCategory varchar(10) PATH '$.simulatorCategory',
                      n_cleaningCategory varchar(20) PATH '$.cleaningCategory',
                      n_date varchar(10) PATH '$.date',
                      n_findings text PATH '$.findings',
                      n_hasDescription varchar(5) PATH '$.hasDescription',
                      n_hasIntervention varchar(5) PATH '$.hasIntervention',
                      n_intervention json PATH '$.intervention',
                      n_userName varchar(120) PATH '$.user.name'
                    )
                  )) j
                  UNION ALL
                  SELECT l.CreatedAt, l.UpdatedAt, j.simulatorName, j.simulatorCode, j.simulatorCategory, j.cleaningCategory,
                    j.date, j.findings, j.hasDescription, j.hasIntervention, j.intervention, j.userName
                  FROM cleaning_reports_legacy l,
                  JSON_TABLE(l.Data, '$' COLUMNS (
                    simulatorName varchar(120) PATH '$.simulatorName',
                    simulatorCode varchar(40) PATH '$.simulatorCode',
                    simulatorCategory varchar(10) PATH '$.simulatorCategory',
                    cleaningCategory varchar(20) PATH '$.cleaningCategory',
                    date varchar(10) PATH '$.date',
                    findings text PATH '$.findings',
                    hasDescription varchar(5) PATH '$.hasDescription',
                    hasIntervention varchar(5) PATH '$.hasIntervention',
                    intervention json PATH '$.intervention',
                    userName varchar(120) PATH '$.user.name'
                  )) j
                  WHERE JSON_EXTRACT(l.Data, '$.simulator') IS NULL
                ) c
                WHERE c.SimulatorName IS NOT NULL AND c.CleaningDate IS NOT NULL;");

            migrationBuilder.DropTable(name: "cleaning_reports_legacy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "cleaning_reports", newName: "cleaning_reports_new");

            migrationBuilder.CreateTable(
                name: "cleaning_reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Data = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: true),
                    UserName = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserRole = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cleaning_reports", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(name: "IX_cleaning_reports_CreatedByUserId", table: "cleaning_reports", column: "CreatedByUserId");
            migrationBuilder.CreateIndex(name: "IX_cleaning_reports_ReportDate", table: "cleaning_reports", column: "ReportDate");
            migrationBuilder.CreateIndex(name: "IX_cleaning_reports_UserName", table: "cleaning_reports", column: "UserName");

            // Volta como documentos planos (um por limpeza).
            migrationBuilder.Sql(@"
                INSERT INTO cleaning_reports (Id, Data, ReportDate, UserName, UserRole, CreatedByUserId, CreatedAt, UpdatedAt)
                SELECT r.Id,
                  JSON_OBJECT('date', DATE_FORMAT(r.CleaningDate, '%d-%m-%Y'), 'cleaningCategory', r.CleaningCategory,
                    'simulatorCategory', r.SimulatorCategory, 'simulatorName', r.SimulatorName, 'simulatorCode', r.SimulatorCode,
                    'findings', r.Findings, 'hasDescription', r.HasDescription IS TRUE, 'hasIntervention', r.HasIntervention IS TRUE,
                    'intervention', r.Intervention, 'simulatorImage', s.Image,
                    'user', JSON_OBJECT('name', r.UserName, 'role', u.Role)),
                  r.CleaningDate, r.UserName, u.Role, r.UserId, r.CreatedAt, r.UpdatedAt
                FROM cleaning_reports_new r
                LEFT JOIN simulators s ON s.Id = r.SimulatorId
                LEFT JOIN users u ON u.Id = r.UserId;");

            migrationBuilder.DropTable(name: "cleaning_reports_new");
        }
    }
}
