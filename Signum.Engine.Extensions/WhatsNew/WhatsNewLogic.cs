using Signum.Engine.Authorization;
using Signum.Engine.Files;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.WhatsNew;
using System.Globalization;

namespace Signum.Engine.WhatsNew;

public static class WhatsNewLogic
{
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
                .WithSave(WhatsNewOperation.Save)
                .WithDelete(WhatsNewOperation.Delete)
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Status,
                    e.Name,
                    e.CreationDate,
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

            sb.Schema.EntityEvents<WhatsNewEntity>().PreUnsafeDelete += WhatsNewLogic_PreUnsafeDelete;

            FileTypeLogic.Register(WhatsNewFileType.WhatsNewAttachmentFileType, previewFileTypeAlgorithm);
            FileTypeLogic.Register(WhatsNewFileType.WhatsNewPreviewFileType, attachmentsFileTypeAlgorithm);

            Validator.PropertyValidator((WhatsNewEntity wn) => wn.Messages).StaticPropertyValidation += (WhatsNewEntity wn, PropertyInfo pi) =>
            {
                var defaultCulture = (Schema.Current.ForceCultureInfo?.Name ?? "en");
                if (!wn.Messages.Any(m => m.Culture.Name == defaultCulture))
                {
                    return WhatsNewMessage._0ContiansNoVersionForCulture1.NiceToString(pi.NiceName(), defaultCulture);
                }
                return null;
            };

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
                CanBeNew = true,
                Execute = (e, _) => { e.Status = WhatsNewState.Draft; },
            }.Register();
        } 
    }
}
