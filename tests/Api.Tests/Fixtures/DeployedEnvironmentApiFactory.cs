using Microsoft.Extensions.Hosting;

namespace Api.Tests.Fixtures;

/// <summary>
/// Hosts the API as if it were deployed rather than on a developer's machine, so tests can prove
/// the things that are refused outside Development actually are.
/// </summary>
public class DeployedEnvironmentApiFactory : BudgetApiFactory
{
    protected override string Environment => Environments.Production;
}
