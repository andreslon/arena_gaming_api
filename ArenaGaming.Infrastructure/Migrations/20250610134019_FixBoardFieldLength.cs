using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaGaming.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixBoardFieldLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Board",
                table: "Games",
                type: "character(9)",
                fixedLength: true,
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(9)",
                oldMaxLength: 9);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Board",
                table: "Games",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character(9)",
                oldFixedLength: true,
                oldMaxLength: 9);
        }
    }
}
