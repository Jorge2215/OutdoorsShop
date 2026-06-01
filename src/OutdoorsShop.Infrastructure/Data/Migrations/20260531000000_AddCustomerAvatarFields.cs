using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutdoorsShop.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerAvatarFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarContentType",
                table: "Customers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarPath",
                table: "Customers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarContentType",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "AvatarPath",
                table: "Customers");
        }
    }
}
