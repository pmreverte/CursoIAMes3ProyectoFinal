using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sprint2.Data.Migrations
{
    /// <summary>
    /// Represents the initial migration for creating the Tasks table.
    /// </summary>
    public partial class InitialCreate : Migration
    {
        /// <summary>
        /// Builds the operations that will migrate the database 'up' to this migration.
        /// </summary>
        /// <param name="migrationBuilder">The builder used to construct the migration operations.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Creates the Tasks table with specified columns and constraints
            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    // Primary key column with auto-increment
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    
                    // Description column with a maximum length of 200 characters
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    
                    // Status column to store the task status as an integer
                    Status = table.Column<int>(type: "int", nullable: false),
                    
                    // CreatedAt column to store the creation date and time
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    
                    // DueDate column to store the due date, nullable
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    
                    // Priority column to store the task priority as an integer
                    Priority = table.Column<int>(type: "int", nullable: false),
                    
                    // Category column with a maximum length of 50 characters, nullable
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    
                    // Notes column with a maximum length of 500 characters, nullable
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    // Sets the primary key constraint on the Id column
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                });
        }

        /// <summary>
        /// Builds the operations that will migrate the database 'down' from this migration.
        /// </summary>
        /// <param name="migrationBuilder">The builder used to construct the migration operations.</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drops the Tasks table
            migrationBuilder.DropTable(
                name: "Tasks");
        }
    }
}
