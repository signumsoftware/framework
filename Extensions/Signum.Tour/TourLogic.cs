using Signum.UserAssets;

namespace Signum.Tour;

public static class TourLogic
{
    public static IEnumerable<CustomTourSymbol> RegisteredCustomTours
    {
        get { return customTours; }
    }

    static HashSet<CustomTourSymbol> customTours = new HashSet<CustomTourSymbol>();

    public static void RegisterCustomTours(params CustomTourSymbol[] tours)
    {
        foreach (var t in tours)
        {
            if (t == null)
                throw AutoInitAttribute.ArgumentNullException(typeof(CustomTourSymbol), nameof(tours));

            TourLogic.customTours.Add(t);
        }
    }


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
                e.Target,
                e.ShowProgress,
                e.Animate,
                e.ShowCloseButton,
            });

        SymbolLogic<CustomTourSymbol>.Start(sb, () => RegisteredCustomTours.ToHashSet());

        UserAssetsImporter.Register<TourEntity>("Tour", TourOperation.Save);
    }
}
