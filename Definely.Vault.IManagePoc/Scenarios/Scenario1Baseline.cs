using Definely.Vault.IManagePoc.Auth;
using Definely.Vault.IManagePoc.Data;
using Microsoft.Extensions.Configuration;

namespace Definely.Vault.IManagePoc.Scenarios;

public class Scenario1Baseline : IScenario
{
    public string Name => "Scenario 1: Current Solution Baseline (Sequential)";

    public Task RunAsync(PocDbContext db, HttpClient httpClient, iManageAuthClient authClient,
        IConfigurationSection config, CancellationToken cancellationToken)
    {
        Console.WriteLine("Not yet implemented.");
        return Task.CompletedTask;
    }
}
