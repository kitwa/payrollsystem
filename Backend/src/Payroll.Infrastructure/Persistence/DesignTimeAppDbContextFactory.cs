using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Payroll.Infrastructure.Persistence;

public sealed class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var apiPath = Directory.Exists(Path.Combine(currentDirectory, "src", "Payroll.Api"))
            ? Path.Combine(currentDirectory, "src", "Payroll.Api")
            : Path.GetFullPath(Path.Combine(currentDirectory, "..", "Payroll.Api"));
        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be supplied through configuration or environment variables.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                connectionString,
                new MariaDbServerVersion(new Version(10, 11, 0)),
                mysql => mysql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options);
    }
}