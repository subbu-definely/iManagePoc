using Definely.Vault.IManagePoc.Auth;
using Definely.Vault.IManagePoc.Data;
using Microsoft.Extensions.Configuration;

namespace Definely.Vault.IManagePoc.Scenarios;

public interface IScenario
{
    string Name { get; }
    Task RunAsync(PocDbContext db, HttpClient httpClient, iManageAuthClient authClient,
        IConfiguration config, CancellationToken cancellationToken);
}
