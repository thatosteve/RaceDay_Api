using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RaceDay_Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ROLES",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    role_name = table.Column<string>(type: "varchar(20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROLES", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "USERS",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    role_id = table.Column<int>(type: "int", nullable: false),
                    first_name = table.Column<string>(type: "varchar(50)", nullable: false),
                    last_name = table.Column<string>(type: "varchar(50)", nullable: false),
                    email = table.Column<string>(type: "varchar(150)", nullable: false),
                    password_hash = table.Column<string>(type: "varchar(255)", nullable: false),
                    phone_number = table.Column<string>(type: "varchar(20)", nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "date", nullable: true),
                    profile_image_url = table.Column<string>(type: "varchar(500)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USERS", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_USERS_ROLES",
                        column: x => x.role_id,
                        principalTable: "ROLES",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EVENTS",
                columns: table => new
                {
                    event_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    organiser_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(150)", nullable: false),
                    description = table.Column<string>(type: "varchar(1000)", nullable: true),
                    event_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    location = table.Column<string>(type: "varchar(200)", nullable: false),
                    distance_km = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    event_type = table.Column<string>(type: "varchar(10)", nullable: false),
                    banner_image_url = table.Column<string>(type: "varchar(500)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EVENTS", x => x.event_id);
                    table.CheckConstraint("CK_EVENTS_TYPE", "event_type IN ('run', 'walk', 'cycle')");
                    table.ForeignKey(
                        name: "FK_EVENTS_USERS",
                        column: x => x.organiser_id,
                        principalTable: "USERS",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CATEGORIES",
                columns: table => new
                {
                    category_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    event_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", nullable: false),
                    category_type = table.Column<string>(type: "varchar(20)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CATEGORIES", x => x.category_id);
                    table.CheckConstraint("CK_CATEGORIES_TYPE", "category_type IN ('Age', 'Distance')");
                    table.ForeignKey(
                        name: "FK_CATEGORIES_EVENTS",
                        column: x => x.event_id,
                        principalTable: "EVENTS",
                        principalColumn: "event_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ENROLMENTS",
                columns: table => new
                {
                    enrolment_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    event_id = table.Column<int>(type: "int", nullable: false),
                    category_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "Pending"),
                    enrolled_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ENROLMENTS", x => x.enrolment_id);
                    table.CheckConstraint("CK_ENROLMENTS_STATUS", "status IN ('Pending', 'Confirmed')");
                    table.ForeignKey(
                        name: "FK_ENROLMENTS_CATEGORIES",
                        column: x => x.category_id,
                        principalTable: "CATEGORIES",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ENROLMENTS_EVENTS",
                        column: x => x.event_id,
                        principalTable: "EVENTS",
                        principalColumn: "event_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ENROLMENTS_USERS",
                        column: x => x.user_id,
                        principalTable: "USERS",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RESULTS",
                columns: table => new
                {
                    result_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    enrolment_id = table.Column<int>(type: "int", nullable: false),
                    finish_time = table.Column<TimeSpan>(type: "time", nullable: true),
                    finish_position = table.Column<int>(type: "int", nullable: true),
                    recorded_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RESULTS", x => x.result_id);
                    table.ForeignKey(
                        name: "FK_RESULTS_ENROLMENTS",
                        column: x => x.enrolment_id,
                        principalTable: "ENROLMENTS",
                        principalColumn: "enrolment_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ROLES",
                columns: new[] { "role_id", "role_name" },
                values: new object[,]
                {
                    { 1, "Organiser" },
                    { 2, "Participant" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORIES_event_id",
                table: "CATEGORIES",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_ENROLMENTS_category_id",
                table: "ENROLMENTS",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_ENROLMENTS_event_id",
                table: "ENROLMENTS",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "UQ_ENROLMENTS",
                table: "ENROLMENTS",
                columns: new[] { "user_id", "event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EVENTS_organiser_id",
                table: "EVENTS",
                column: "organiser_id");

            migrationBuilder.CreateIndex(
                name: "UQ_RESULTS_enrolment_id",
                table: "RESULTS",
                column: "enrolment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_ROLES_role_name",
                table: "ROLES",
                column: "role_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USERS_role_id",
                table: "USERS",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "UQ_USERS_email",
                table: "USERS",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RESULTS");

            migrationBuilder.DropTable(
                name: "ENROLMENTS");

            migrationBuilder.DropTable(
                name: "CATEGORIES");

            migrationBuilder.DropTable(
                name: "EVENTS");

            migrationBuilder.DropTable(
                name: "USERS");

            migrationBuilder.DropTable(
                name: "ROLES");
        }
    }
}
