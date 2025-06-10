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
            // Custom SQL to handle the conversion from char[] to string
            migrationBuilder.Sql(@"
                -- First, add a temporary column
                ALTER TABLE ""Games"" ADD COLUMN ""Board_temp"" character varying(9);
                
                -- Convert the array to string (handling the case where we have character arrays)
                UPDATE ""Games"" 
                SET ""Board_temp"" = CASE 
                    WHEN ""Board"" IS NULL THEN '         '
                    ELSE array_to_string(""Board"", '')
                END;
                
                -- Drop the old column
                ALTER TABLE ""Games"" DROP COLUMN ""Board"";
                
                -- Rename the temporary column
                ALTER TABLE ""Games"" RENAME COLUMN ""Board_temp"" TO ""Board"";
                
                -- Make it NOT NULL
                ALTER TABLE ""Games"" ALTER COLUMN ""Board"" SET NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Custom SQL to handle the conversion back from string to char[]
            migrationBuilder.Sql(@"
                -- Add temporary column
                ALTER TABLE ""Games"" ADD COLUMN ""Board_temp"" character(1)[];
                
                -- Convert string back to char array
                UPDATE ""Games"" 
                SET ""Board_temp"" = string_to_array(""Board"", NULL)::character(1)[];
                
                -- Drop the string column
                ALTER TABLE ""Games"" DROP COLUMN ""Board"";
                
                -- Rename temp column back
                ALTER TABLE ""Games"" RENAME COLUMN ""Board_temp"" TO ""Board"";
                
                -- Make it NOT NULL
                ALTER TABLE ""Games"" ALTER COLUMN ""Board"" SET NOT NULL;
            ");
        }
    }
}
