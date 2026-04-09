using Definely.Vault.IManagePoc.Auth;
using Definely.Vault.IManagePoc.Data;
using Definely.Vault.IManagePoc.Scenarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Definely.Vault.IManagePoc;

internal class Program
{
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = config.GetConnectionString("PocDatabase")!;
        var imanageConfig = config.GetSection("IManage");

        // Set up database
        var optionsBuilder = new DbContextOptionsBuilder<PocDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        await using var db = new PocDbContext(optionsBuilder.Options);
        await db.Database.EnsureCreatedAsync();
        Console.WriteLine("[DB] Database ready.");

        // Set up auth client
        var httpClient = new HttpClient();
        var authClient = new iManageAuthClient(
            httpClient,
            imanageConfig["AuthUrl"]!,
            imanageConfig["Username"]!,
            imanageConfig["Password"]!,
            imanageConfig["ClientId"]!,
            imanageConfig["ClientSecret"]!);

        // Test authentication
        await authClient.GetAccessTokenAsync();
        Console.WriteLine("[Auth] Authenticated successfully.");

        // Available scenarios
        var scenarios = new Dictionary<string, IScenario>
        {
            ["1"] = new Scenario1Baseline(),
            ["2"] = new Scenario2Optimised(),
            ["3"] = new Scenario3SyncApi(),
            ["4"] = new Scenario4ChangeEvents()
        };

        // Menu
        Console.WriteLine();
        Console.WriteLine("=== iManage Import POC ===");
        Console.WriteLine();
        Console.WriteLine("Select scenario:");
        Console.WriteLine("  1 - Current Solution Baseline (sequential)");
        Console.WriteLine("  2 - Optimised Current APIs (parallel + in-memory tree)");
        Console.WriteLine("  3 - Sync API (bulk crawl)");
        Console.WriteLine("  4 - Change Events (incremental sync)");
        Console.WriteLine("  r - Reset database (delete all data)");
        Console.WriteLine("  q - Quit");
        Console.WriteLine();

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine()?.Trim();

            if (input == "q" || input == "Q")
            {
                Console.WriteLine("Done.");
                return;
            }

            if (input == "r" || input == "R")
            {
                Console.Write("Are you sure? This will delete ALL data. (y/n): ");
                var confirm = Console.ReadLine()?.Trim();
                if (confirm == "y" || confirm == "Y")
                {
                    await db.Database.EnsureDeletedAsync();
                    await db.Database.EnsureCreatedAsync();
                    Console.WriteLine("[DB] Database reset complete.");
                }
                continue;
            }

            if (scenarios.TryGetValue(input!, out var scenario))
            {
                Console.WriteLine($"\nRunning: {scenario.Name}\n");
                try
                {
                    using var cts = new CancellationTokenSource();
                    await scenario.RunAsync(db, httpClient, authClient, config, cts.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[Error] Scenario failed: {ex.Message}");
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("Invalid selection. Enter 1, 2, 3, 4, or q.");
            }
        }
    }
}
