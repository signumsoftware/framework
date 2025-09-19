using Signum.Authorization;
using Signum.Authorization.Rules;
using Signum.Files;
using System.Collections.Frozen;
using System.Globalization;

namespace Signum.WhatsNew;

public static class WhatsNewLogic
{

    static ResetLazy<FrozenDictionary<Lite<WhatsNewEntity>, WhatsNewEntity>> WhatsNews = null!; //remove 
    static Dictionary<Type, IRelatedConfig> RelatedConfigDictionary = new Dictionary<Type, IRelatedConfig>();

    [AutoExpressionField]
    public static bool IsRead(this WhatsNewEntity wn) => 
        As.Expression(() => Administrator.QueryDisableAssertAllowed<WhatsNewLogEntity>().Any(log => log.WhatsNew.Is(wn) && log.User.Is(UserEntity.Current)));

    [AutoExpressionField]
    public static IQueryable<WhatsNewLogEntity> WhatsNewLogs(this WhatsNewEntity wn) =>
        As.Expression(() => Database.Query<WhatsNewLogEntity>().Where(log => log.WhatsNew.Is(wn)));

    public static void Start(SchemaBuilder sb, 
        IFileTypeAlgorithm previewFileTypeAlgorithm,
        IFileTypeAlgorithm attachmentsFileTypeAlgorithm)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            sb.Include<WhatsNewEntity>()
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Status,
                    e.Name,
                    e.CreationDate,
                    e.Related
                });

            sb.Include<WhatsNewLogEntity>()
               .WithExpressionFrom((WhatsNewEntity wn) => wn.WhatsNewLogs())
               .WithDelete(WhatsNewLogOperation.Delete)
               .WithQuery(() => e => new
               {
                   Entity = e,
                   e.Id,
                   e.ReadOn,
                   e.User,
                   e.WhatsNew,
               });

            QueryLogic.Expressions.Register((WhatsNewEntity wn) => wn.IsRead(), WhatsNewMessage.IsRead);

            sb.Schema.EntityEvents<WhatsNewEntity>().PreUnsafeDelete += WhatsNewLogic_PreUnsafeDelete;

            WhatsNews = sb.GlobalLazy(() => Database.Query<WhatsNewEntity>().ToFrozenDictionary(a => a.ToLite()),
                new InvalidateWith(typeof(WhatsNewEntity)));

            FileTypeLogic.Register(WhatsNewFileType.WhatsNewAttachmentFileType, previewFileTypeAlgorithm);
            FileTypeLogic.Register(WhatsNewFileType.WhatsNewPreviewFileType, attachmentsFileTypeAlgorithm);

            Validator.PropertyValidator((WhatsNewEntity wn) => wn.Messages).StaticPropertyValidation += (WhatsNewEntity wn, PropertyInfo pi) =>
            {
                var ci = Schema.Current.ForceCultureInfo;
                var defaultCulture = ci == null ? "en" : ci.IsNeutralCulture ? ci.Name : ci.Parent.Name;
                if (!wn.Messages.Any(m => m.Culture.Name == defaultCulture))
                {
                    return WhatsNewMessage._0ContiansNoVersionForCulture1.NiceToString(pi.NiceName(), defaultCulture);
                }
                return null;
            };

            RegisterRelatedConfig<QueryEntity>(lite => IsQueryAllowed(lite));

            RegisterRelatedConfig<PermissionSymbol>(lite => PermissionAuthLogic.IsAuthorized(SymbolLogic<PermissionSymbol>.ToSymbol(lite.ToString()!)));

            WhatsNewGraph.Register();
        }
    }

    private static IDisposable? WhatsNewLogic_PreUnsafeDelete(IQueryable<WhatsNewEntity> query)
    {
        query.SelectMany(wn => wn.WhatsNewLogs()).UnsafeDelete();
        return null;
    }

    public static WhatsNewMessageEmbedded GetCurrentMessage(this WhatsNewEntity wn)
    {
        return wn.Messages.SingleOrDefault(entity => entity.Culture.Name == CultureInfo.CurrentCulture.Name) ??
        wn.Messages.SingleOrDefault(entity => entity.Culture.Name == CultureInfo.CurrentCulture.Parent.Name) ??
        wn.Messages.SingleOrDefault(entity => entity.Culture.Name == (Schema.Current.ForceCultureInfo?.Name ?? "en")) ??
        wn.Messages.FirstEx();
    }

    public static void RegisterPublishedTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
    {
        TypeConditionLogic.RegisterCompile<WhatsNewEntity>(typeCondition, wn => wn.Status == WhatsNewState.Publish);
    }


    public class WhatsNewGraph : Graph<WhatsNewEntity, WhatsNewState>
    {
        public static void Register()
        {
            GetState = f => f.Status;

            new Graph<WhatsNewEntity>.Execute(WhatsNewOperation.Save)
            {
                CanBeModified = true,
                CanBeNew = true,
                Execute = (e, _) => {  },
            }.Register();

            new Execute(WhatsNewOperation.Publish)
            {
                FromStates = { WhatsNewState.Draft },
                ToStates = { WhatsNewState.Publish },
                CanBeNew = true,
                Execute = (e, _) => { e.Status = WhatsNewState.Publish; },
            }.Register();

            new Execute(WhatsNewOperation.Unpublish)
            {
                FromStates = { WhatsNewState.Publish },
                ToStates = { WhatsNewState.Draft },
                Execute = (e, _) => { e.Status = WhatsNewState.Draft; },
            }.Register();

            new Graph<WhatsNewEntity>.Delete(WhatsNewOperation.Delete)
            {
                Delete = (e, _) => { e.Delete(); },
            }.Register();
        } 
    }

    static bool IsQueryAllowed(Lite<QueryEntity> query)
    {
        try
        {
            return QueryLogic.Queries.QueryAllowed(QueryLogic.QueryNames.GetOrThrow(query.ToString()!), true);
        }
        catch (Exception e) when (StartParameters.IgnoredDatabaseMismatches != null)
        {
            //Could happen when not 100% synchronized
            StartParameters.IgnoredDatabaseMismatches.Add(e);

            return false;
        }
    }

    public static List<(WhatsNewEntity wn, bool isRead)> GetWhatNews()
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<WhatsNewEntity>(userInterface: false);

        var read = AuthLogic.Disable().Using(() => Database.Query<WhatsNewLogEntity>().Where(a => a.User.Is(UserEntity.Current)).Select(a => a.WhatsNew).ToHashSet());

        return WhatsNews.Value.Values
            .Where(wn => isAllowed(wn))
            .Where(wn => wn.Related == null || RelatedConfigDictionary.GetOrThrow(wn.Related.EntityType).IsAuhorized(wn.Related))
            .Select(wn => (wn, isRead: read.Contains(wn.ToLite())))
            .ToList();
    }

    public static WhatsNewEntity? GetWhatNew(int id)
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<WhatsNewEntity>(userInterface: false);

        return WhatsNews.Value.Values
            .Where(wn => wn.Id == id)
            .Where(wn => isAllowed(wn))
            .Where(wn => wn.Related == null || RelatedConfigDictionary.GetOrThrow(wn.Related.EntityType).IsAuhorized(wn.Related))
            .SingleOrDefault();
    }

    public static void RegisterRelatedConfig<T>(Func<Lite<T>, bool> isAuthorized) where T : Entity
    {
        RelatedConfigDictionary.Add(typeof(T), new RelatedConfig<T>(isAuthorized));
    }

    public static RelatedConfig<T> GetRelatedConfig<T>() where T : Entity
    {
        return (RelatedConfig<T>)RelatedConfigDictionary.GetOrThrow(typeof(T));
    }


    public interface IRelatedConfig
    {
        bool IsAuhorized(Lite<Entity> lite);
    }

    public class RelatedConfig<T> : IRelatedConfig where T : Entity
    {
        public Func<Lite<T>, bool> IsAuthorized;

        public RelatedConfig(Func<Lite<T>, bool> isAuthorized)
        {
            IsAuthorized = isAuthorized;
        }

        bool IRelatedConfig.IsAuhorized(Lite<Entity> lite) => IsAuthorized((Lite<T>)lite);
    }
}
