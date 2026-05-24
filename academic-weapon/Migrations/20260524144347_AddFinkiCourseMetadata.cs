using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace academic_weapon.Migrations
{
    /// <inheritdoc />
    public partial class AddFinkiCourseMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcademicYear",
                table: "Subjects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "Subjects",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CourseUrl",
                table: "Subjects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SemesterType",
                table: "Subjects",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcademicYear",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "CourseUrl",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "SemesterType",
                table: "Subjects");
        }
    }
}
