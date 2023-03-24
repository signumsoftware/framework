using Signum.Chart.UserChart;
using Signum.Entities.UserAssets;
using Signum.Files;
using Signum.UserQueries;

namespace Signum.Dashboard;

[EntityKind(EntityKind.System, EntityData.Master)]
public class CachedQueryEntity : Entity
{
    public Lite<DashboardEntity> Dashboard { get; set; }

    [PreserveOrder, NoRepeatValidator]
    [ImplementedBy(typeof(UserQueryEntity), typeof(UserChartEntity))]
    public MList<Lite<IUserAssetEntity>> UserAssets { get; set; } = new MList<Lite<IUserAssetEntity>>();

    [DefaultFileType(nameof(CachedQueryFileType.CachedQuery), nameof(CachedQueryFileType))]
    public FilePathEmbedded File { get; set; }

    public int NumRows { get; set; }

    public int NumColumns { get; set; }

    public DateTime CreationDate { get; internal set; }

    [Unit("ms")]
    public long QueryDuration { get; set; }
    
    [Unit("ms")]
    public long UploadDuration { get; set; }
}

[AutoInit]
public static class CachedQueryFileType
{
    public static FileTypeSymbol CachedQuery;
}

