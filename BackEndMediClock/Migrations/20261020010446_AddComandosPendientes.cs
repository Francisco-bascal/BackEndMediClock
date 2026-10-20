using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackEndMediClock.Migrations
{
    /// <inheritdoc />
    public partial class AddComandosPendientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConfiguracionPendiente",
                table: "Dispositivos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DispensarPendiente",
                table: "Dispositivos",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfiguracionPendiente",
                table: "Dispositivos");

            migrationBuilder.DropColumn(
                name: "DispensarPendiente",
                table: "Dispositivos");
        }
    }
}
