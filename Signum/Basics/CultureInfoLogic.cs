using Signum.Utilities.Reflection;
using System.Globalization;
using Signum.Engine.Maps;
using Signum.Engine.Sync;
using System.Collections.Frozen;

namespace Signum.Basics;

public static class CultureInfoLogic
{
    public static CultureInfo ToCultureInfo(this CultureInfoEntity ci)
    {
        return EntityToCultureInfo.Value.GetOrThrow(ci);
    }

    public static void AssertStarted(SchemaBuilder sb)
    {
        sb.AssertDefined(ReflectionTools.GetMethodInfo(() => CultureInfoLogic.Start(null!)));
    }

    public static Func<CultureInfo, CultureInfo> CultureInfoModifier = ci => ci;

    public static ResetLazy<FrozenDictionary<string, CultureInfoEntity>> CultureInfoToEntity = null!;
    public static ResetLazy<FrozenDictionary<CultureInfoEntity, CultureInfo>> EntityToCultureInfo = null!;

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<CultureInfoEntity>()
            .WithSave(CultureInfoOperation.Save)
            .WithDelete(CultureInfoOperation.Delete)
            .WithQuery(() => c => new
            {
                Entity = c,
                c.Id,
                c.Name,
                c.EnglishName,
                c.NativeName,
            });

        CultureInfoToEntity = sb.GlobalLazy(() => Database.Query<CultureInfoEntity>().ToFrozenDictionaryEx(ci => ci.Name,
            ci => ci),
            invalidateWith: new InvalidateWith(typeof(CultureInfoEntity)));

        EntityToCultureInfo = sb.GlobalLazy(() => Database.Query<CultureInfoEntity>().ToFrozenDictionaryEx(ci => ci,
            ci => CultureInfoModifier(CultureInfo.GetCultureInfo(ci.Name))),
            invalidateWith: new InvalidateWith(typeof(CultureInfoEntity)));

        sb.Schema.Synchronizing += Schema_Synchronizing;

        if (sb.WebServerBuilder != null)
            CultureServer.Start(sb.WebServerBuilder);
    }

    static SqlPreCommand? Schema_Synchronizing(Replacements rep)
    {
        var cis = Database.Query<CultureInfoEntity>().ToList();

        var table = Schema.Current.Table(typeof(CultureInfoEntity));

        using (rep.WithReplacedDatabaseName())
            return cis.Select(c => table.UpdateSqlSync(c, ci => ci.Name == c.Name)).Combine(Spacing.Double);
    }

    public static CultureInfoEntity ToCultureInfoEntity(this CultureInfo ci)
    {
        return CultureInfoToEntity.Value.GetOrThrow(ci.Name);
    }

    public static CultureInfoEntity? TryGetCultureInfoEntity(this CultureInfo ci)
    {
        return CultureInfoToEntity.Value.TryGetC(ci.Name);
    }

    public static IEnumerable<CultureInfo> ApplicationCultures(bool? isNeutral) => EntityToCultureInfo.Value.Values.Where(a=>isNeutral == null || a.IsNeutralCulture == isNeutral);
    
    public static IEnumerable<T> ForEachCulture<T>(Func<CultureInfoEntity, T> func)
    {
        if (EntityToCultureInfo.Value.Count == 0)
            throw new InvalidOperationException("No {0} found in the database".FormatWith(typeof(CultureInfoEntity).Name));

        foreach (var c in EntityToCultureInfo.Value.Where(a => a.Value.IsNeutralCulture))
        {
            using (CultureInfoUtils.ChangeBothCultures(c.Value))
            {
                yield return func(c.Key);
            }
        }
    }

    public static CultureInfoEntity GetCultureInfoEntity(string cultureName)
    {
        return CultureInfoToEntity.Value.GetOrThrow(cultureName);
    }
}
