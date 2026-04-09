using Definely.Vault.IManagePoc.Auth;
using Definely.Vault.IManagePoc.Data;
using Microsoft.Extensions.Configuration;

namespace Definely.Vault.IManagePoc.Scenarios;

public class Scenario4ChangeEvents : IScenario
{
    public string Name => "Scenario 4: Change Events (Incremental Sync)";

    public Task RunAsync(PocDbContext db, HttpClient httpClient, iManageAuthClient authClient,
        IConfigurationSection config, CancellationToken cancellationToken)
    {
        Console.WriteLine("Not yet implemented.");
        return Task.CompletedTask;
    }
}
