using Definely.Vault.IManagePoc.Auth;
using Definely.Vault.IManagePoc.Data;
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
        var authClient = new IManageAuthClient(
            httpClient,
            imanageConfig["AuthUrl"]!,
            imanageConfig["Username"]!,
            imanageConfig["Password"]!,
            imanageConfig["ClientId"]!,
            imanageConfig["ClientSecret"]!);

        // Test authentication
        var token = await authClient.GetAccessTokenAsync();
        Console.WriteLine($"[Auth] Authenticated successfully.");

        // Menu
        Console.WriteLine();
        Console.WriteLine("=== iManage Import POC ===");
        Console.WriteLine();
        Console.WriteLine("Select scenario:");
        Console.WriteLine("  1 - Current Solution Baseline (sequential)");
        Console.WriteLine("  2 - Optimised Current APIs (parallel + in-memory tree)");
        Console.WriteLine("  3 - Sync API (bulk crawl)");
        Console.WriteLine("  4 - Change Events (incremental sync)");
        Console.WriteLine("  q - Quit");
        Console.WriteLine();

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine()?.Trim();

            switch (input)
            {
                case "1":
                    Console.WriteLine("Scenario 1: Current Solution Baseline — not yet implemented");
                    // TODO: await Scenario1.RunAsync(db, httpClient, authClient, imanageConfig);
                    break;
                case "2":
                    Console.WriteLine("Scenario 2: Optimised Current APIs — not yet implemented");
                    // TODO: await Scenario2.RunAsync(db, httpClient, authClient, imanageConfig);
                    break;
                case "3":
                    Console.WriteLine("Scenario 3: Sync API — not yet implemented");
                    // TODO: await Scenario3.RunAsync(db, httpClient, authClient, imanageConfig);
                    break;
                case "4":
                    Console.WriteLine("Scenario 4: Change Events — not yet implemented");
                    // TODO: await Scenario4.RunAsync(db, httpClient, authClient, imanageConfig);
                    break;
                case "q":
                case "Q":
                    Console.WriteLine("Done.");
                    return;
                default:
                    Console.WriteLine("Invalid selection. Enter 1, 2, 3, 4, or q.");
                    break;
            }
        }
    }
}
