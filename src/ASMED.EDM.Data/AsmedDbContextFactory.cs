using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ASMED.EDM.Data;

/// <summary>
/// Factory dla EF Core Design-Time Tools (migracje, scaffolding)
/// </summary>
public class AsmedDbContextFactory : IDesignTimeDbContextFactory<AsmedDbContext>
{
    public AsmedDbContext CreateDbContext(string[] args)
    {
        // Hardcoded connection string dla migracji design-time
        // W produkcji używamy DatabaseConnectionService
        var connectionString = "Server=mysql84.nq.pl;Database=asmed2026_krone;User=asmed_krone;Password=!Asmed2020;CharSet=utf8mb4;";

        var optionsBuilder = new DbContextOptionsBuilder<AsmedDbContext>();

        optionsBuilder.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 4, 0)));

        return new AsmedDbContext(optionsBuilder.Options);
    }
}
