using Microsoft.Extensions.Diagnostics.HealthChecks;
using Signum.API;
using Signum.Basics;
using Signum.Dashboard;
using Signum.UserAssets;
using Signum.UserQueries;
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

        sb.Schema.EntityEvents<PropertyRouteEntity>().PreDeleteSqlSync += property =>
            Administrator.UnsafeDeletePreCommandMList((TourStepEntity ts) => ts.CssSteps, Database.MListQuery((TourStepEntity ts) => ts.CssSteps).Where(mle => mle.Element.Property.Is(property)));

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

        sb.Schema.EntityEvents<DashboardEntity>().Saved += (dashboard, args) =>
        {
            if (args.WasNew)
                return;

            var dashboardLite = dashboard.ToLite();
            var validGuids = dashboard.Parts.Select(p => p.Guid).ToHashSet();

            // Drop CssStep rows whose part-Guid no longer exists in the saved dashboard.
            Database.MListQuery((TourStepEntity ts) => ts.CssSteps)
                .Where(mle => mle.Parent.Tour.Entity.Trigger.Is(dashboardLite)
                    && mle.Element.Type == CssStepType.DashboardPart
                    && mle.Element.DashboardPart != null
                    && !validGuids.Contains(mle.Element.DashboardPart.Value))
                .UnsafeDeleteMList();
        };

        sb.Schema.EntityEvents<DashboardEntity>().PreUnsafeDelete += query =>
        {
            query.SelectMany(d => Database.Query<TourEntity>().Where(t => t.Trigger.Is(d.ToLite()))).UnsafeDelete();
            return null;
        };

        sb.Schema.EntityEvents<UserQueryEntity>().PreUnsafeDelete += query =>
        {
            query.SelectMany(uq => Database.Query<TourEntity>().Where(t => t.Trigger.Is(uq.ToLite()))).UnsafeDelete();
            return null;
        };
    }
}
