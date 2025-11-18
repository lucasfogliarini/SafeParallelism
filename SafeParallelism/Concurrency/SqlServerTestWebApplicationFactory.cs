using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;
using Xunit;

namespace SafeParallelism.Concurrency;

public class SqlServerTestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Test123456!")
        .WithCleanUp(true)
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            services.RemoveAll(typeof(DbContextOptions<IssueDbContext>));
            services.RemoveAll(typeof(IssueDbContext));

            // Add the test database context
            services.AddDbContext<IssueDbContext>(options =>
            {
                options.UseSqlServer(_msSqlContainer.GetConnectionString(),
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure();
                        sqlOptions.CommandTimeout(60);
                    });
                options.EnableSensitiveDataLogging();
                options.LogTo(Console.WriteLine, LogLevel.Information);
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync();

        // Wait a bit for the container to be fully ready
        await Task.Delay(2000);

        // Create the database schema
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IssueDbContext>();

        try
        {
            await dbContext.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating database: {ex.Message}");
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _msSqlContainer.StopAsync();
        }
        finally
        {
            await _msSqlContainer.DisposeAsync();
        }
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IssueDbContext>();

        try
        {
            // Use raw SQL to truncate the table (faster than delete)
            await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Issues");
        }
        catch
        {
            // Fallback to delete if truncate fails
            await dbContext.Issues.ExecuteDeleteAsync();
        }
    }
}