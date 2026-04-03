using Signum.Dashboard;
using Signum.UserQueries;

namespace Signum.Tree;

public static class UserTreePartLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        DashboardLogic.PartNames.AddRange(new Dictionary<string, Type>
        {
            {"UserTreePart", typeof(UserTreePartEntity)},
        });

        DashboardLogic.OnGetCachedQueryDefinition.Register((UserTreePartEntity ute, PanelPartEmbedded pp) => Array.Empty<CachedQueryDefinition>());
    }
}
