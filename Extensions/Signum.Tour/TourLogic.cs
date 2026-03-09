using Microsoft.Extensions.Diagnostics.HealthChecks;
using Signum.API;
using Signum.Basics;
using Signum.UserAssets;
using System.Collections.Frozen;
using System.IO;

namespace Signum.Tour;

public static class TourLogic
{
    public static ResetLazy<FrozenDictionary<Lite<Entity>, TourEntity>> ToursByTrigger = null!;

    public static IEnumerable<TourTriggerSymbol> RegisteredTourTriggers
    {
        get { return tourTriggers; }
    }

    static HashSet<TourTriggerSymbol> tourTriggers = new HashSet<TourTriggerSymbol>();

    public static void RegisterTourTriggers(params TourTriggerSymbol[] tours)
    {
        foreach (var t in tours)
        {
            if (t == null)
                throw AutoInitAttribute.ArgumentNullException(typeof(TourTriggerSymbol), nameof(tours));

            TourLogic.tourTriggers.Add(t);
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
                e.Trigger,
                e.ShowProgress,
                e.Animate,
                e.ShowCloseButton,
            });

        SymbolLogic<TourTriggerSymbol>.Start(sb, () => RegisteredTourTriggers.ToHashSet());

        ToursByTrigger = sb.GlobalLazy(() =>
            Database.Query<TourEntity>().ToFrozenDictionaryEx(a => a.Trigger),
            new InvalidateWith(typeof(TourEntity)));

        if (sb.WebServerBuilder != null)
        {
            EntityPackTS.AddExtension += pack =>
            {
                var tour = ToursByTrigger.Value.TryGetC(pack.entity.GetType().ToTypeEntity().ToLite());
                if (tour != null)
                    pack.extension.Add("hasTour", true);
            };
        }

        UserAssetsImporter.Register<TourEntity>("Tour", TourOperation.Save);
    }
}
