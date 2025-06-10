using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaGaming.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeBoardToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Board",
                table: "Games",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(char[]),
                oldType: "character(1)[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<char[]>(
                name: "Board",
                table: "Games",
                type: "character(1)[]",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(9)",
                oldMaxLength: 9);
        }
    }
}
