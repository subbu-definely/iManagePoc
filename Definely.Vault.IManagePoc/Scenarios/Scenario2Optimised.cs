using Definely.Vault.IManagePoc.Auth;
using Definely.Vault.IManagePoc.Data;
using Microsoft.Extensions.Configuration;

namespace Definely.Vault.IManagePoc.Scenarios;

public class Scenario2Optimised : IScenario
{
    public string Name => "Scenario 2: Optimised Current APIs (Parallel + In-Memory Tree)";

    public Task RunAsync(PocDbContext db, HttpClient httpClient, iManageAuthClient authClient,
        IConfiguration config, CancellationToken cancellationToken)
    {
        Console.WriteLine("Not yet implemented.");
        return Task.CompletedTask;
    }
}
