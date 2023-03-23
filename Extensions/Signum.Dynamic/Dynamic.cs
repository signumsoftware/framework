using Signum.Entities.Authorization;

namespace Signum.Entities.Dynamic;

[AutoInit]
public static class DynamicPanelPermission
{
    public static PermissionSymbol ViewDynamicPanel;
    public static PermissionSymbol RestartApplication;
}

public enum DynamicPanelMessage
{
    OpenErrors
}
