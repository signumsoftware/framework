using Signum.UserAssets;

namespace Signum.Tour;

public static class TourLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<TourEntity>()
            .WithSave(TourOperation.Save)
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.Name,
                e.ShowProgress,
                e.Animate,
                e.ShowCloseButton,
            });

        UserAssetsImporter.Register<TourEntity>("Tour", TourOperation.Save);
    }
}
