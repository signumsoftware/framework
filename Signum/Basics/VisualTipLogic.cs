using Signum.Engine.Maps;

namespace Signum.Basics;

public static class VisualTipLogic
{
    public static HashSet<VisualTipSymbol> VisualTipSymbols = new HashSet<VisualTipSymbol>();
    private static Func<bool>? isVisualTipConsumeEnabled;

    public static void Start(SchemaBuilder sb, Func<bool>? isVisualTipConsumeEnabled = null)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        VisualTipLogic.isVisualTipConsumeEnabled = isVisualTipConsumeEnabled;
        sb.Include<VisualTipConsumedEntity>()
            .WithUniqueIndex(vt => new {vt.VisualTip, vt.User })
            .WithDelete(VisualTipConsumedOperation.Delete)
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.VisualTip,
                e.User,
                e.ConsumedOn
            });

        SymbolLogic<VisualTipSymbol>.Start(sb, () => VisualTipSymbols);

        RegisterType(typeof(SearchVisualTip));
    }

    public static void RegisterType(Type visualTipContainer)
    {
        foreach (var fi in visualTipContainer.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var val = (VisualTipSymbol?)fi.GetValue(null);

            if (val == null)
                throw AutoInitAttribute.ArgumentNullException(typeof(VisualTipSymbol), fi.Name);

            VisualTipSymbols.Add(val);
        }
    }

    public static void RegisterVisualTipSymbol(VisualTipSymbol visualTip)
    {
        if (visualTip == null)
            throw AutoInitAttribute.ArgumentNullException(typeof(VisualTipSymbol), nameof(visualTip));

        VisualTipSymbols.Add(visualTip);
    }

   public static List<VisualTipSymbol>? GetConsumed()
    {
        if (isVisualTipConsumeEnabled != null && !isVisualTipConsumeEnabled())
            return null;

        using (ExecutionMode.Global())
            return Database.Query<VisualTipConsumedEntity>().Where(vt => vt.User.Is(UserHolder.Current.User)).Select(vt => vt.VisualTip).ToList();
    }

    public static void Consume(string symbolKey)
    {
        using (ExecutionMode.Global())
        {
            var symbol = SymbolLogic<VisualTipSymbol>.ToSymbol(symbolKey);
            if (!Database.Query<VisualTipConsumedEntity>().Any(vt => vt.User.Is(UserHolder.Current.User) && vt.VisualTip.Is(symbol)))
                new VisualTipConsumedEntity
                {
                    User = UserHolder.Current.User,
                    VisualTip = symbol,
                    ConsumedOn = Clock.Now
                }.Save();
        }
    }
}
