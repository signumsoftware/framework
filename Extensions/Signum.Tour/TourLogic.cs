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
            .WithDelete(TourOperation.Delete)
            .WithVirtualMList(a=>a.Steps, s => s.Tour)
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                Name = e.ToString(),
                e.ForEntity,
                e.ShowProgress,
                e.Animate,
                e.ShowCloseButton,
            });

        UserAssetsImporter.Register<TourEntity>("Tour", TourOperation.Save);
    }
}
