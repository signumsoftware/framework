using DocumentFormat.OpenXml.Spreadsheet;
using Signum.Authorization;
using Signum.Scheduler;
using Schema = Signum.Engine.Maps.Schema;

namespace Signum.Mailing.Reception;

public static class EmailReceptionLogic
{


    [AutoExpressionField]
    public static TimeSpan? Duration(this EmailReceptionEntity e) =>
    As.Expression(() => (TimeSpan?)(e!.EndDate - e.StartDate));


    [AutoExpressionField]
    public static IQueryable<EmailReceptionEntity> Receptions(this EmailReceptionConfigurationEntity c) =>
        As.Expression(() => Database.Query<EmailReceptionEntity>().Where(r => r.EmailReceptionConfiguration.Is(c)));

    [AutoExpressionField]
    public static IQueryable<EmailMessageEntity> EmailMessages(this EmailReceptionEntity r) =>
        As.Expression(() => Database.Query<EmailMessageEntity>().Where(m => m.Mixin<EmailReceptionMixin>().ReceptionInfo!.Reception.Is(r)));

    [AutoExpressionField]
    public static IQueryable<ExceptionEntity> Exceptions(this EmailReceptionEntity e) =>
        As.Expression(() => Database.Query<EmailReceptionExceptionEntity>().Where(a => a.Reception.Is(e)).Select(a => a.Exception.Entity));

    [AutoExpressionField]
    public static EmailReceptionEntity? Pop3Reception(this ExceptionEntity ex) =>
        As.Expression(() => Database.Query<EmailReceptionExceptionEntity>().Where(re => re.Exception.Is(ex)).Select(re => re.Reception.Entity).SingleOrDefaultEx());

    public static Action<EmailReceptionEntity>? ReceptionComunication;

    public static bool IsStarted = false;

    public static Polymorphic<Func< EmailReceptionServiceEntity, EmailReceptionConfigurationEntity, ScheduledTaskContext, EmailReceptionEntity>> EmailReceptionServices = 
        new Polymorphic<Func<EmailReceptionServiceEntity, EmailReceptionConfigurationEntity, ScheduledTaskContext, EmailReceptionEntity>>();

    public static void Start(SchemaBuilder sb, Func<EmailReceptionConfigurationEntity, EmailReceptionEntity> getPop3Client, Func<string, string>? encryptPassword = null, Func<string, string>? decryptPassword = null)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;


        MixinDeclarations.AssertDeclared(typeof(EmailMessageEntity), typeof(EmailReceptionMixin));

        sb.Include<EmailReceptionConfigurationEntity>()
            .WithQuery(() => s => new
            {
                Entity = s,
                s.Id,
                s.Active,
                s.Service, 
            });

        sb.Include<EmailReceptionEntity>();
        sb.Include<EmailReceptionExceptionEntity>();

        sb.Include<EmailMessageEntity>()
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.From,
                e.Subject,
                e.Template,
                e.State,
                e.Sent,
                SentDate = (DateTime?)e.Mixin<EmailReceptionMixin>().ReceptionInfo!.SentDate,
                e.Exception,
            });

        QueryLogic.Queries.Register(typeof(EmailReceptionEntity), () => DynamicQueryCore.Auto(
        from s in Database.Query<EmailReceptionEntity>()
        select new
        {
            Entity = s,
            s.Id,
            s.EmailReceptionConfiguration,
            s.StartDate,
            s.EndDate,
            s.NewEmails,
            EmailMessages = s.EmailMessages().Count(),
            Exceptions = s.Exceptions().Count(),
            s.Exception,
        })
        .ColumnDisplayName(a => a.EmailMessages, () => typeof(EmailMessageEntity).NicePluralName())
        .ColumnDisplayName(a => a.Exceptions, () => typeof(ExceptionEntity).NicePluralName()));

        QueryLogic.Expressions.Register((EmailReceptionConfigurationEntity c) => c.Receptions(), () => typeof(EmailReceptionEntity).NicePluralName());
        QueryLogic.Expressions.Register((EmailReceptionEntity r) => r.EmailMessages(), () => typeof(EmailMessageEntity).NicePluralName());
        QueryLogic.Expressions.Register((EmailReceptionEntity r) => r.Exceptions(), () => typeof(ExceptionEntity).NicePluralName());
        QueryLogic.Expressions.Register((EmailReceptionEntity r) => r.Duration(), () => typeof(TimeSpan).NiceName());
        QueryLogic.Expressions.Register((ExceptionEntity r) => r.Pop3Reception(), () => typeof(EmailReceptionEntity).NiceName());

        new Graph<EmailReceptionConfigurationEntity>.Execute(EmailReceptionConfigurationOperation.Save)
        {
            CanBeNew = true,
            CanBeModified = true,
            Execute = (e, _) => { }
        }.Register();

        new Graph<EmailReceptionEntity>.ConstructFrom<EmailReceptionConfigurationEntity>(EmailReceptionConfigurationOperation.ReceiveEmails)
        {
            CanBeNew = true,
            CanBeModified = true,
            Construct = (e, args) =>
            {
                using (var tr = Transaction.None())
                {
                    ScheduledTaskLogEntity stl = new ScheduledTaskLogEntity
                    {
                        Task = EmailReceptionAction.ReceiveAllActiveEmailConfigurations,
                        //ScheduledTask = scheduledTask,
                        StartTime = Clock.Now,
                        MachineName = Schema.Current.MachineName,
                        ApplicationName = Schema.Current.ApplicationName,
                        User = UserEntity.Current,
                    };
                    var ctx = new ScheduledTaskContext(stl);




                    EmailReceptionEntity result = ReceiveEmails( e, ctx);
                    return tr.Commit(result);
                }
            }
        }.Register();

        SchedulerLogic.ExecuteTask.Register((EmailReceptionConfigurationEntity conf, ScheduledTaskContext ctx) => ReceiveEmails(conf,ctx).ToLite());

        SimpleTaskLogic.Register(EmailReceptionAction.ReceiveAllActiveEmailConfigurations, (ScheduledTaskContext ctx) =>
        {
            if (!EmailLogic.Configuration.ReciveEmails)
                throw new InvalidOperationException("EmailLogic.Configuration.ReciveEmails is set to false");

            foreach (var item in Database.Query<EmailReceptionConfigurationEntity>().Where(a => a.Active).ToList())
            {
                ctx.CancellationToken.ThrowIfCancellationRequested();
                ReceiveEmails(item, ctx);
            }

            return null;
        });

        sb.Schema.SchemaCompleted += () =>
        {
            var pr = PropertyRoute.Construct((EmailReceptionConfigurationEntity s) => s.Service);

            var implementations = sb.Schema.FindImplementations(PropertyRoute.Construct((EmailReceptionConfigurationEntity s) => s.Service));

            var notOverriden = implementations.Types.Except(EmailReceptionServices.OverridenTypes);

            if (notOverriden.Any())
            {
                throw new InvalidOperationException($"The property {pr} is implemented by {notOverriden.CommaAnd(a => a.TypeName())} but has not been registered in EmailReceptionLogic.EmailSenders. Maybe you forgot to call something like {notOverriden.CommaAnd(a => a.TypeName().Replace("Entity", "Logic.Start(sb)"))}?");
            }
        };

        IsStarted = true;
    }

    private static EmailReceptionEntity ReceiveEmails(EmailReceptionConfigurationEntity e, ScheduledTaskContext ctx)
    {

    

        return EmailReceptionServices.Invoke(e.Service, e, ctx);
    }

}


