using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoginCheck.Infrastructure.Migrations;

public partial class AddTechnicianFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.Users', N'Phone') IS NULL
                    ALTER TABLE [dbo].[Users] ADD [Phone] nvarchar(30) NOT NULL CONSTRAINT [DF_Users_Phone] DEFAULT N'';
                IF COL_LENGTH(N'dbo.Users', N'Role') IS NULL
                    ALTER TABLE [dbo].[Users] ADD [Role] nvarchar(30) NOT NULL CONSTRAINT [DF_Users_Role] DEFAULT N'User';
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.Users', N'Phone') IS NOT NULL
                    ALTER TABLE [dbo].[Users] DROP COLUMN [Phone];
                IF COL_LENGTH(N'dbo.Users', N'Role') IS NOT NULL
                    ALTER TABLE [dbo].[Users] DROP COLUMN [Role];
            END
            """);
    }
}
