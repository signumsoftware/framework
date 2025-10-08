using Signum.DynamicQuery.Tokens;
using Signum.Processes;
using Signum.Templating;
using Signum.Utilities.Reflection;
using System.Collections.Frozen;
using System.Globalization;

namespace Signum.SMS;


public static class SMSLogic
{
    public static SMSTemplateMessageEmbedded? GetCultureMessage(this SMSTemplateEntity template, CultureInfo ci)
    {
        return template.Messages.SingleOrDefault(tm => tm.CultureInfo.ToCultureInfo() == ci);
    }

    [AutoExpressionField]
    public static IQueryable<SMSMessageEntity> SMSMessages(this ISMSOwnerEntity e) => 
        As.Expression(() => Database.Query<SMSMessageEntity>().Where(m => m.Referred.Is(e)));

    [AutoExpressionField]
    public static IQueryable<SMSMessageEntity> SMSMessages(this SMSSendPackageEntity e) => 
        As.Expression(() => Database.Query<SMSMessageEntity>().Where(a => a.SendPackage.Is(e)));

    [AutoExpressionField]
    public static IQueryable<SMSMessageEntity> SMSMessages(this SMSUpdatePackageEntity e) => 
        As.Expression(() => Database.Query<SMSMessageEntity>().Where(a => a.UpdatePackage.Is(e)));



    static Func<SMSConfigurationEmbedded> getConfiguration = null!;
    public static SMSConfigurationEmbedded Configuration
    {
        get { return getConfiguration(); }
    }

    public static ISMSProvider? Provider { get; set; }

    public static ISMSProvider GetProvider() => Provider ?? throw new InvalidOperationException("No ISMSProvider set");

    public static ResetLazy<FrozenDictionary<Lite<SMSTemplateEntity>, SMSTemplateEntity>> SMSTemplatesLazy = null!;
    public static ResetLazy<FrozenDictionary<object, List<SMSTemplateEntity>>> SMSTemplatesByQueryName = null!;

    public static void AssertStarted(SchemaBuilder sb)
    {
        sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null!, null!, null!)));
    }

    public static void Start(SchemaBuilder sb, ISMSProvider? provider, Func<SMSConfigurationEmbedded> getConfiguration)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        CultureInfoLogic.AssertStarted(sb);
        sb.Schema.SchemaCompleted += () => Schema_SchemaCompleted(sb);

        SMSLogic.getConfiguration = getConfiguration;
        SMSLogic.Provider = provider;

        sb.Include<SMSMessageEntity>()
            .WithQuery(() => m => new
            {
                Entity = m,
                m.Id,
                m.From,
                m.DestinationNumber,
                m.State,
                m.SendDate,
                m.Template,
                m.Referred,
                m.Exception,
            });

        sb.Include<SMSTemplateEntity>()
            .WithUniqueIndex(t => new { t.Model }, where: t => t.Model != null && t.IsActive == true)
            .WithQuery(() => t => new
            {
                Entity = t,
                t.Id,
                t.Name,
                t.IsActive,
                t.From,
                t.Query,
                t.Model,
            });


        sb.Schema.EntityEvents<SMSTemplateEntity>().PreSaving += new PreSavingEventHandler<SMSTemplateEntity>(EmailTemplate_PreSaving);
        sb.Schema.EntityEvents<SMSTemplateEntity>().Retrieved += SMSTemplateLogic_Retrieved;
        sb.Schema.EntityEvents<SMSModelEntity>().PreDeleteSqlSync += e =>
            Administrator.UnsafeDeletePreCommand(Database.Query<SMSTemplateEntity>()
                .Where(a => a.Model.Is(e)));

        SMSTemplatesLazy = sb.GlobalLazy(() =>
            Database.Query<SMSTemplateEntity>().ToFrozenDictionary(et => et.ToLite())
            , new InvalidateWith(typeof(SMSTemplateEntity)));

        SMSTemplatesByQueryName = sb.GlobalLazy(() =>
        {
            return SMSTemplatesLazy.Value.Values.Where(q=>q.Query!=null).SelectCatch(et => KeyValuePair.Create(et.Query!.ToQueryName(), et)).GroupToFrozenDictionary();
        }, new InvalidateWith(typeof(SMSTemplateEntity)));


        SMSMessageGraph.Register();
        SMSTemplateGraph.Register();

        Validator.PropertyValidator<SMSTemplateEntity>(et => et.Messages).StaticPropertyValidation += (t, pi) =>
        {

            var dc = SMSLogic.Configuration?.DefaultCulture;

            if (dc != null && !t.Messages.Any(m => m.CultureInfo.Is(dc)))
                return SMSTemplateMessage.ThereMustBeAMessageFor0.NiceToString().FormatWith(dc.EnglishName);

            return null;
        };

        ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;
        ExceptionLogic.DeleteLogs += ExceptionLogic_DeletePackages;
    }

    public static void ExceptionLogic_DeletePackages(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
    {
        Database.Query<SMSSendPackageEntity>().Where(pack => !Database.Query<ProcessEntity>().Any(pr => pr.Data == pack) && !pack.SMSMessages().Any())
            .UnsafeDeleteChunksLog(parameters, sb, token);

        Database.Query<SMSUpdatePackageEntity>().Where(pack => !Database.Query<ProcessEntity>().Any(pr => pr.Data == pack) && !pack.SMSMessages().Any())
            .UnsafeDeleteChunksLog(parameters, sb, token);
    }

    public static void ExceptionLogic_DeleteLogs(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
    {
        void Remove(DateTime? dateLimit, bool withExceptions)
        {
            if (dateLimit == null)
                return;

            var query = Database.Query<SMSMessageEntity>().Where(o => o.SendDate != null && o.SendDate < dateLimit);

            if (withExceptions)
                query = query.Where(a => a.Exception != null);

            query.UnsafeDeleteChunksLog(parameters, sb, token);
        }

        Remove(parameters.GetDateLimitDelete(typeof(SMSMessageEntity).ToTypeEntity()), withExceptions: false);
        Remove(parameters.GetDateLimitDeleteWithExceptions(typeof(SMSMessageEntity).ToTypeEntity()), withExceptions: true);
    }

    public static HashSet<Type> GetAllTypes()
    {
        return TypeLogic.TypeToEntity
                  .Where(kvp => typeof(ISMSOwnerEntity).IsAssignableFrom(kvp.Key))
                  .Select(kvp => kvp.Key)
                  .ToHashSet();
    }

    public static void SMSTemplateLogic_Retrieved(SMSTemplateEntity smsTemplate, PostRetrievingContext ctx)
    {
        if (smsTemplate.Query == null)
            return;

        using (smsTemplate.DisableAuthorization ? ExecutionMode.Global() : null)
        {
            object queryName = QueryLogic.ToQueryName(smsTemplate.Query!.Key);
            QueryDescription description = QueryLogic.Queries.QueryDescription(queryName);

            using (smsTemplate.DisableAuthorization ? ExecutionMode.Global() : null)
                smsTemplate.ParseData(description);
        }
    }


    static void EmailTemplate_PreSaving(SMSTemplateEntity smsTemplate, PreSavingContext ctx)
    {
        if (smsTemplate.Query == null)
            return;

        using (smsTemplate.DisableAuthorization ? ExecutionMode.Global() : null)
        {
            var queryName = QueryLogic.ToQueryName(smsTemplate.Query!.Key);
            QueryDescription qd = QueryLogic.Queries.QueryDescription(queryName);

            List<QueryToken> list = new List<QueryToken>();

            foreach (var message in smsTemplate.Messages)
            {
                message.Message = TextTemplateParser.Parse(message.Message, qd, smsTemplate.Model?.ToType()).ToString();
            }
        }
    }

    static string CheckLength(string result, SMSTemplateEntity template)
    {
        if (template.RemoveNoSMSCharacters)
        {
            result = SMSCharacters.RemoveNoSMSCharacters(result);
        }

        int remainingLength = SMSCharacters.RemainingLength(result);
        if (remainingLength < 0)
        {
            switch (template.MessageLengthExceeded)
            {
                case MessageLengthExceeded.NotAllowed:
                    throw new ApplicationException(SMSCharactersMessage.TheTextForTheSMSMessageExceedsTheLengthLimit.NiceToString());
                case MessageLengthExceeded.Allowed:
                    break;
                case MessageLengthExceeded.TextPruning:
                    return result.RemoveEnd(Math.Abs(remainingLength));
            }
        }

        return result;
    }

    public static void SendSMS(SMSMessageEntity message)
    {
        if (!message.DestinationNumber.Contains(','))
        {
            SendOneMessage(message);
        }
        else
        {
            var numbers = message.DestinationNumber.Split(',').Select(n => n.Trim()).Distinct();
            message.DestinationNumber = numbers.FirstEx();
            SendOneMessage(message);
            foreach (var number in numbers.Skip(1))
            {
                SendOneMessage(new SMSMessageEntity
                {
                    DestinationNumber = number,
                    Certified = message.Certified,
                    EditableMessage = message.EditableMessage,
                    From = message.From,
                    Message = message.Message,
                    Referred = message.Referred,
                    State = SMSMessageState.Created,
                    Template = message.Template,
                    SendPackage = message.SendPackage,
                    UpdatePackage = message.UpdatePackage,
                    UpdatePackageProcessed = message.UpdatePackageProcessed,
                });
            }
        }
    }

    private static void SendOneMessage(SMSMessageEntity message)
    {
        using (OperationLogic.AllowSave<SMSMessageEntity>())
        {
            try
            {
                message.MessageID = GetProvider().SMSSendAndGetTicket(message);
                message.SendDate = Clock.Now.TruncSeconds();
                message.State = SMSMessageState.Sent;
                message.Save();

            }
            catch (Exception e)
            {
                var ex = e.LogException();
                using (var tr = Transaction.ForceNew())
                {
                    message.Exception = ex.ToLite();
                    message.State = SMSMessageState.SendFailed;
                    message.Save();
                    tr.Commit();
                }
                throw;
            }
        }
    }

    public static void SendAsyncSMS(SMSMessageEntity message)
    {
        Task.Factory.StartNew(() =>
        {
            SendSMS(message);
        });
    }

    public static List<SMSMessageEntity> CreateAndSendMultipleSMSMessages(MultipleSMSModel template, List<string> phones)
    {
        var messages = new List<SMSMessageEntity>();
        var IDs = GetProvider().SMSMultipleSendAction(template, phones);
        var sendDate = Clock.Now.TruncSeconds();
        for (int i = 0; i < phones.Count; i++)
        {
            var message = new SMSMessageEntity { Message = template.Message, From = template.From };
            message.SendDate = sendDate;
            //message.SendState = SendState.Sent;
            message.DestinationNumber = phones[i];
            message.MessageID = IDs[i];
            message.Save();
            messages.Add(message);
        }

        return messages;
    }
    
    public static SMSMessageEntity CreateSMSMessage(Lite<SMSTemplateEntity> template, Entity? entity, ISMSModel? model, CultureInfo? forceCulture)
    {
        var t = SMSLogic.SMSTemplatesLazy.Value.GetOrThrow(template);

        var defaultCulture = SMSLogic.Configuration.DefaultCulture.ToCultureInfo();

        if (t.Query != null)
        {
            var qd = QueryLogic.Queries.QueryDescription(t.Query.ToQueryName());

            List<QueryToken> tokens = new List<QueryToken>();
            t.ParseData(qd);

            tokens.Add(t.To!.Token);

            var parsedNodes = t.Messages.ToDictionary(
                tm => tm.CultureInfo.ToCultureInfo(),
                tm => TextTemplateParser.Parse(tm.Message, qd, t.Model?.ToType())
            );

            parsedNodes.Values.ToList().ForEach(n => n.FillQueryTokens(tokens));

            var columns = tokens.Distinct().Select(qt => new Column(qt, null)).ToList();

            var filters = model != null ? model.GetFilters(qd) :
                new List<Filter> { new FilterCondition(QueryUtils.Parse("Entity", qd, 0), FilterOperation.EqualTo, entity!.ToLite()) };


            var table = QueryLogic.Queries.ExecuteQuery(new QueryRequest
            {
                QueryName = qd.QueryName,
                Columns = columns,
                Pagination = model?.GetPagination() ?? new Pagination.All(),
                Filters = filters,
                Orders = model?.GetOrders(qd) ?? new List<Order>(),
            });

            var columnTokens = table.Columns.ToDictionary(a => a.Token);

            var ownerData = (SMSOwnerData)table.Rows[0][columnTokens.GetOrThrow(t.To!.Token)]!;

            var ci = forceCulture ?? ownerData.CultureInfo?.ToCultureInfo() ?? defaultCulture;

            var node = parsedNodes.TryGetC(ci) ?? parsedNodes.GetOrThrow(defaultCulture);
            return new SMSMessageEntity
            {
                Template = t.ToLite(),
                Message = node.Print(new TextTemplateParameters(entity, ci, new QueryContext(qd, table)) { Model = model }),
                From = t.From,
                EditableMessage = t.EditableMessage,
                State = SMSMessageState.Created,
                Referred = ownerData.Owner,
                DestinationNumber = ownerData.TelephoneNumber,
                Certified = t.Certified
            };
        }
        else
        {

            var ci = (forceCulture ?? defaultCulture).ToCultureInfoEntity();

            return new SMSMessageEntity
            {
                Template = t.ToLite(),
                Message = t.Messages.Where(m=>  m.CultureInfo.Is(ci)).SingleEx().Message,
                From = t.From,
                EditableMessage = t.EditableMessage,
                State = SMSMessageState.Created,
                Certified = t.Certified
            };


        }


     
    }

    private static void Schema_SchemaCompleted(SchemaBuilder sb)
    {
        var types = sb.Schema.Tables.Keys.Where(t => typeof(ISMSOwnerEntity).IsAssignableFrom(t));

        foreach (var type in types)
            giRegisterSMSMessagesExpression.GetInvoker(type)(sb);
    }

    static GenericInvoker<Action<SchemaBuilder>> giRegisterSMSMessagesExpression = new(sb => RegisterSMSMessagesExpression<ISMSOwnerEntity>(sb));
    private static void RegisterSMSMessagesExpression<T>(SchemaBuilder sb)
        where T : ISMSOwnerEntity
    {
        QueryLogic.Expressions.Register((T a) => a.SMSMessages(), () => typeof(SMSMessageEntity).NicePluralName());
    }
}

public class SMSMessageGraph : Graph<SMSMessageEntity, SMSMessageState>
{

    public static void Register()
    {
        GetState = m => m.State;

        new ConstructFrom<SMSTemplateEntity>(SMSMessageOperation.CreateSMSFromTemplate)
        {
            CanConstruct = t => !t.IsActive ? SMSCharactersMessage.TheTemplateMustBeActiveToConstructSMSMessages.NiceToString() : null,
            ToStates = { SMSMessageState.Created },
            Construct = (t, args) =>
            {
                return SMSLogic.CreateSMSMessage(t.ToLite(),
                    args.TryGetArgC<Lite<Entity>>()?.RetrieveAndRemember(),
                    args.TryGetArgC<ISMSModel>(),
                    args.TryGetArgC<CultureInfo>());
            }
        }.Register();

        new Execute(SMSMessageOperation.Send)
        {
            CanBeNew = true,
            CanBeModified = true,
            FromStates = { SMSMessageState.Created },
            ToStates = { SMSMessageState.Sent },
            Execute = (m, _) =>
            {
                try
                {
                    SMSLogic.SendSMS(m);
                }
                catch (Exception e)
                {
                    var ex = e.LogException();
                    using (var tr = Transaction.ForceNew())
                    {
                        m.Exception = ex.ToLite();
                        m.Save();
                        tr.Commit();
                    }
                    throw;
                }
            }
        }.Register();


        new Graph<SMSMessageEntity>.Execute(SMSMessageOperation.UpdateStatus)
        {
            CanExecute = m => m.State != SMSMessageState.Created ? null : SMSCharactersMessage.StatusCanNotBeUpdatedForNonSentMessages.NiceToString(),
            Execute = (sms, args) =>
            {
                var func = args.TryGetArgC<Func<SMSMessageEntity, SMSMessageState>>();
                if (func == null)
                    func = SMSLogic.GetProvider().SMSUpdateStatusAction;

                sms.State = func(sms);

                if (sms.UpdatePackage != null)
                    sms.UpdatePackageProcessed = true;
            }
        }.Register();
    }
}

public class SMSTemplateGraph : Graph<SMSTemplateEntity>
{
    public static void Register()
    {
        new Construct(SMSTemplateOperation.Create)
        {
            Construct = _ => new SMSTemplateEntity
            {
                Messages =
                {
                    new SMSTemplateMessageEmbedded
                    {
                        CultureInfo = SMSLogic.Configuration.DefaultCulture
                    }
                }
            },
        }.Register();

        new Execute(SMSTemplateOperation.Save)
        {
            CanBeModified = true,
            CanBeNew = true,
            Execute = (t, _) => { }
        }.Register();
    }
}

public interface ISMSProvider
{
    string SMSSendAndGetTicket(SMSMessageEntity message);
    List<string> SMSMultipleSendAction(MultipleSMSModel template, List<string> phones);
    SMSMessageState SMSUpdateStatusAction(SMSMessageEntity message);
}
