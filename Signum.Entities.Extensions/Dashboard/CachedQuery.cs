using Signum.Entities.Chart;
using Signum.Entities.Files;
using Signum.Entities.UserAssets;
using Signum.Entities.UserQueries;

namespace Signum.Entities.Dashboard;

[EntityKind(EntityKind.System, EntityData.Master), TicksColumn(false)]
public class CachedQueryEntity : Entity
{
    public Lite<DashboardEntity> Dashboard { get; set; }

    [ImplementedBy(typeof(UserQueryEntity), typeof(UserChartEntity))]
    public Lite<IUserAssetEntity> UserAsset { get; set; }

    [DefaultFileType(nameof(CachedQueryFileType.CachedQuery), nameof(CachedQueryFileType))]
    public FilePathEmbedded File { get; set; }
}

[AutoInit]
public static class CachedQueryFileType
{
    public static FileTypeSymbol CachedQuery;
}

