using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeHub.Migrations
{
    /// <inheritdoc />
    public partial class AddSavingsGoalColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "color",
                table: "savings_goals",
                type: "varchar(15)",
                unicode: false,
                maxLength: 15,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropColumn(name: "color", table: "savings_goals");
        }
    }
}
