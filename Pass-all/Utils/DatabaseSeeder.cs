using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Passall.Utils;

public static class DatabaseSeeder
{
    /// <summary>
    /// Adds a "SuperAdmin" user with default password to the database
    /// </summary>
    /// <param name="migrationBuilder">Migration builder used for insertion</param>
    public static void CreateSuperAdmin(MigrationBuilder migrationBuilder)
    {
        Guid settingsId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        string encryptedPassword = Utils.Hash(Constantes.SuperAdmin_Password) ?? "";

        // Insert default settings for the super admin
        migrationBuilder.InsertData(
            table: "Settings",
            columns: new[] { "Id", "Version" },
            values: new object[] { settingsId, Constantes.Settings_DefaultVersion }
        );

        // Insert the super admin user
        migrationBuilder.InsertData(
            table: "User",
            columns: new[] { "Id", "Login", "Name", "Email", "Password", "SettingsId" },
            values: new object[] { userId, Constantes.SuperAdmin_Login, Constantes.SuperAdmin_Name, Constantes.SuperAdmin_Email, encryptedPassword, settingsId }
        );
    }
}
