using Signum.Scheduler;

namespace Signum.Authorization.AzureAD;

public enum AzureADQuery
{
    ActiveDirectoryUsers,
    ActiveDirectoryGroups,
}

[AutoInit]
public static class AzureADTask
{
    public static readonly SimpleTaskSymbol DeactivateUsers;
}

