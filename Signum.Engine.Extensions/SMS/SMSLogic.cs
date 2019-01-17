using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Entities.SMS;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Engine.Operations;
using Signum.Engine.Processes;
using Signum.Entities.Processes;
using System.Linq.Expressions;
using Signum.Entities.Basics;
using System.Text.RegularExpressions;
using Signum.Engine.Basics;
using System.Threading.Tasks;
using System.Globalization;

namespace Signum.Engine.SMS
{

    public static class SMSLogic
    {
        public static SMSTemplateMessageEmbedded GetCultureMessage(this SMSTemplateEntity template, CultureInfo ci)
        {
            return template.Messages.SingleOrDefault(tm => tm.CultureInfo.ToCultureInfo() == ci);
        }

        public static Expression<Func<Entity, IQueryable<SMSMessageEntity>>> SMSMessagesExpression =
            e => Database.Query<SMSMessageEntity>().Where(m => m.Referred.Is(e));
        [ExpressionField]
        public static IQueryable<SMSMessageEntity> SMSMessages(this Entity e)
        {
            return SMSMessagesExpression.Evaluate(e);
        }

        static Expression<Func<SMSSendPackageEntity, IQueryable<SMSMessageEntity>>> SMSMessagesSendExpression =
            e => Database.Query<SMSMessageEntity>().Where(a => a.SendPackage.Is(e));
        [ExpressionField]
        public static IQueryable<SMSMessageEntity> SMSMessages(this SMSSendPackageEntity e)
        {
            return SMSMessagesSendExpression.Evaluate(e);
        }

        static Expression<Func<SMSUpdatePackageEntity, IQueryable<SMSMessageEntity>>> SMSMessagesUpdateExpression =
          e => Database.Query<SMSMessageEntity>().Where(a => a.UpdatePackage.Is(e));
        [ExpressionField]
        public static IQueryable<SMSMessageEntity> SMSMessages(this SMSUpdatePackageEntity e)
        {
            return SMSMessagesUpdateExpression.Evaluate(e);
        }



        static Func<SMSConfigurationEmbedded> getConfiguration;
        public static SMSConfigurationEmbedded Configuration
        {
            get { return getConfiguration(); }
        }

        public static ISMSProvider Provider { get; set; }

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null, null, null)));
        }

        public static void Start(SchemaBuilder sb, ISMSProvider provider, Func<SMSConfigurationEmbedded> getConfiguration)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                CultureInfoLogic.AssertStarted(sb);

                SMSLogic.getConfiguration = getConfiguration;
                SMSLogic.Provider = provider;

                sb.Include<SMSMessageEntity>()
                    .WithQuery(() => m => new
                    {
                        Entity = m,
                        m.Id,
                        Source = m.From,
                        m.DestinationNumber,
                        m.State,
                        m.SendDate,
                        m.Template
                    });

                sb.Include<SMSTemplateEntity>()
                    .WithQuery(() => t => new
                    {
                        Entity = t,
                        t.Id,
                        t.Name,
                        IsActive = t.IsActiveNow(),
                        Source = t.From,
                        t.AssociatedType,
                        t.StartDate,
                        t.EndDate,
                    });
                
                SMSMessageGraph.Register();
                SMSTemplateGraph.Register();

                Validator.PropertyValidator<SMSTemplateEntity>(et => et.Messages).StaticPropertyValidation += (t, pi) =>
                {
                    if (!t.Messages.Any(m => m.CultureInfo.Is(SMSLogic.Configuration.DefaultCulture)))
                        return SMSTemplateMessage.ThereMustBeAMessageFor0.NiceToString().FormatWith(SMSLogic.Configuration.DefaultCulture.EnglishName);

                    return null;
                };
            }
        }

        static Dictionary<Type, LambdaExpression> phoneNumberProviders = new Dictionary<Type, LambdaExpression>();
        static Dictionary<Type, LambdaExpression> cultureProviders = new Dictionary<Type, LambdaExpression>();

        public static void RegisterPhoneNumberProvider<T>(Expression<Func<T, string>> phoneExpression, Expression<Func<T, CultureInfo>> cultureExpression) where T : Entity
        {
            phoneNumberProviders[typeof(T)] = phoneExpression;
            cultureProviders[typeof(T)] = cultureExpression;

            new Graph<ProcessEntity>.ConstructFromMany<T>(SMSMessageOperation.SendSMSMessages)
            {
                Construct = (providers, args) =>
                {
                    var numbers = Database.Query<T>().Where(p => providers.Contains(p.ToLite()))
                        .Select(pr => new { Exp = phoneExpression.Evaluate(pr), Referred = pr.ToLite() }).AsEnumerable().NotNull().Distinct().ToList();

                    var splitNumbers = (from p in numbers.Where(p => p.Exp.Contains(','))
                                        from n in p.Exp.Split('n')
                                        select new { Exp = n.Trim(), p.Referred }).Concat(numbers.Where(p => !p.Exp.Contains(','))).Distinct().ToList();

                    numbers = splitNumbers;

                    MultipleSMSModel model = args.GetArg<MultipleSMSModel>();

                    IntegrityCheck ic = model.IntegrityCheck();

                    if (!model.Message.HasText())
                        throw new ApplicationException("The text for the SMS message has not been set");

                    SMSSendPackageEntity package = new SMSSendPackageEntity().Save();

                    var packLite = package.ToLite();

                    using (OperationLogic.AllowSave<SMSMessageEntity>())
                        numbers.Select(n => new SMSMessageEntity
                        {
                            DestinationNumber = n.Exp,
                            SendPackage = packLite,
                            Referred = n.Referred,

                            Message = model.Message,
                            From = model.From,
                            Certified = model.Certified,
                            State = SMSMessageState.Created,
                        }).SaveList();

                    var process = ProcessLogic.Create(SMSMessageProcess.Send, package);

                    process.Execute(ProcessOperation.Execute);

                    return process;
                }
            }.Register();
        }



        public static string GetPhoneNumber<T>(T entity) where T : IEntity
        {
            var phoneFunc = (Expression<Func<T, string>>)phoneNumberProviders
                .GetOrThrow(typeof(T), "{0} is not registered as PhoneNumberProvider".FormatWith(typeof(T).NiceName()));

            return phoneFunc.Evaluate(entity);
        }

        public static CultureInfo GetCulture<T>(T entity) where T : IEntity
        {
            var cultureFunc = (Expression<Func<T, CultureInfo>>)cultureProviders
                .GetOrThrow(typeof(T), "{0} is not registered as CultureProvider".FormatWith(typeof(T).NiceName()));

            return cultureFunc.Evaluate(entity);
        }

        #region Message composition

        static Dictionary<Type, LambdaExpression> dataObjectProviders = new Dictionary<Type, LambdaExpression>();

        public static List<Lite<TypeEntity>> RegisteredDataObjectProviders()
        {
            return dataObjectProviders.Keys.Select(t => t.ToTypeEntity().ToLite()).ToList();
        }

        public static List<string> GetLiteralsFromDataObjectProvider(Type type)
        {
            if (!dataObjectProviders.ContainsKey(type))
                throw new ArgumentOutOfRangeException("The type {0} is not a registered data provider"
                    .FormatWith(type.FullName));

            return dataObjectProviders[type].GetType().GetGenericArguments()[0]
                .GetGenericArguments()[1].GetProperties().Select(p => "{{{0}}}".FormatWith(p.Name)).ToList();
        }

        public static void RegisterDataObjectProvider<T, A>(Expression<Func<T, A>> func) where T : Entity
        {
            dataObjectProviders[typeof(T)] = func;

            new Graph<ProcessEntity>.ConstructFromMany<T>(SMSMessageOperation.SendSMSMessagesFromTemplate)
            {
                Construct = (providers, args) =>
                {
                    var template = args.GetArg<SMSTemplateEntity>();

                    if (TypeLogic.EntityToType[template.AssociatedType] != typeof(T))
                        throw new ArgumentException("The SMS template is associated with the type {0} instead of {1}"
                            .FormatWith(template.AssociatedType.FullClassName, typeof(T).FullName));

                    var phoneFunc = (Expression<Func<T, string>>)phoneNumberProviders
                        .GetOrThrow(typeof(T), "{0} is not registered as PhoneNumberProvider".FormatWith(typeof(T).NiceName()));

                    var cultureFunc = (Expression<Func<T, CultureInfo>>)cultureProviders
                        .GetOrThrow(typeof(T), "{0} is not registered as CultureProvider".FormatWith(typeof(T).NiceName()));

                    var numbers = Database.Query<T>().Where(p => providers.Contains(p.ToLite()))
                          .Select(p => new
                          {
                              Phone = phoneFunc.Evaluate(p),
                              Data = func.Evaluate(p),
                              Referred = p.ToLite(),
                              Culture = cultureFunc.Evaluate(p)
                          }).Where(n => n.Phone.HasText()).AsEnumerable().ToList();

                    var splitdNumbers = (from p in numbers.Where(p => p.Phone.Contains(','))
                                         from n in p.Phone.Split(',')
                                         select new
                                         {
                                             Phone = n.Trim(),
                                             p.Data,
                                             p.Referred,
                                             p.Culture
                                         }).Concat(numbers.Where(p => !p.Phone.Contains(','))).Distinct().ToList();

                    numbers = splitdNumbers;

                    SMSSendPackageEntity package = new SMSSendPackageEntity().Save();
                    var packLite = package.ToLite();

                    using (OperationLogic.AllowSave<SMSMessageEntity>())
                    {
                        numbers.Select(n => new SMSMessageEntity
                        {
                            Message = template.ComposeMessage(n.Data, n.Culture),
                            EditableMessage = template.EditableMessage,
                            From = template.From,
                            DestinationNumber = n.Phone,
                            SendPackage = packLite,
                            State = SMSMessageState.Created,
                            Referred = n.Referred
                        }).SaveList();
                    }

                    var process = ProcessLogic.Create(SMSMessageProcess.Send, package);

                    process.Execute(ProcessOperation.Execute);

                    return process;
                }
            }.Register();

            new Graph<SMSMessageEntity>.ConstructFrom<T>(SMSMessageOperation.CreateSMSWithTemplateFromEntity)
            {
                Construct = (provider, args) =>
                {
                    var template = args.GetArg<SMSTemplateEntity>();

                    if (template.AssociatedType != null &&
                        TypeLogic.EntityToType[template.AssociatedType] != typeof(T))
                        throw new ArgumentException("The SMS template is associated with the type {0} instead of {1}"
                            .FormatWith(template.AssociatedType.FullClassName, typeof(T).FullName));

                    return new SMSMessageEntity
                    {
                        Message = template.ComposeMessage(func.Evaluate(provider), GetCulture(provider)),
                        EditableMessage = template.EditableMessage,
                        From = template.From,
                        DestinationNumber = GetPhoneNumber(provider),
                        State = SMSMessageState.Created,
                        Referred = provider.ToLite(),
                        Certified = template.Certified
                    };
                }
            }.Register();
        }

        static string literalDelimiterStart = "{";
        public static string LiteralDelimiterStart
        {
            get { return literalDelimiterStart; }
        }

        static string literalDelimiterEnd = "}";
        public static string LiteralDelimiterEnd
        {
            get { return literalDelimiterEnd; }
        }


        static Regex literalFinder = new Regex(@"{(?<name>[_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}][_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}\p{Nd}]*)}");

        public static string ComposeMessage(this SMSTemplateEntity template, object o, CultureInfo culture)
        {
            var defaultCulture = SMSLogic.Configuration.DefaultCulture.ToCultureInfo();
            var templateMessage = template.GetCultureMessage(culture ?? defaultCulture) ??
                template.GetCultureMessage(defaultCulture);

            var message = templateMessage.Message;

            if (o == null)
                return message;

            var matches = literalFinder.Matches(message);

            if (matches.Count == 0)
                return message;

            Type t = o.GetType();

            var combinations = (from Match m in literalFinder.Matches(message)
                                select new Combination
                                {
                                    Name = m.Groups["name"].Value,
                                    Value = t.GetProperty(m.Groups["name"].Value)?.Let(fi => fi.GetValue(o, null))?.ToString()
                                }).ToList();

            return CombineText(template, templateMessage, combinations);
        }

        internal class Combination
        {
            public string Name;
            public string Value;
        }

        static string CombineText(SMSTemplateEntity template, SMSTemplateMessageEmbedded templateMessage, List<Combination> combinations)
        {
            string text = templateMessage.Message;

            if (template.RemoveNoSMSCharacters)
            {
                text = SMSCharacters.RemoveNoSMSCharacters(templateMessage.Message);
                combinations.ForEach(c => c.Value = SMSCharacters.RemoveNoSMSCharacters(c.Value));
            }

            return CombineText(text, combinations, template.MessageLengthExceeded);
        }

        static string CombineText(string text, List<Combination> combinations, MessageLengthExceeded onExceeded)
        {
            string result = literalFinder.Replace(text, m => combinations.FirstEx(c => c.Name == m.Groups["name"].Value).Value);
            int remainingLength = SMSCharacters.RemainingLength(result);
            if (remainingLength < 0)
            {
                switch (onExceeded)
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
        #endregion
        
        #region processes

        public static void StartProcesses(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<SMSSendPackageEntity>();
                sb.Include<SMSUpdatePackageEntity>();
                SMSLogic.AssertStarted(sb);
                ProcessLogic.AssertStarted(sb);
                ProcessLogic.Register(SMSMessageProcess.Send, new SMSMessageSendProcessAlgortihm());
                ProcessLogic.Register(SMSMessageProcess.UpdateStatus, new SMSMessageUpdateStatusProcessAlgorithm());

                new Graph<ProcessEntity>.ConstructFromMany<SMSMessageEntity>(SMSMessageOperation.CreateUpdateStatusPackage)
                {
                    Construct = (messages, _) => UpdateMessages(messages.RetrieveFromListOfLite())
                }.Register();

                QueryLogic.Queries.Register(typeof(SMSSendPackageEntity), () =>
                    from e in Database.Query<SMSSendPackageEntity>()
                    let p = e.LastProcess()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        NumLines = e.SMSMessages().Count(),
                        LastProcess = p,
                        NumErrors = e.SMSMessages().Count(s => s.Exception(p) != null),
                    });

                QueryLogic.Queries.Register(typeof(SMSUpdatePackageEntity), () =>
                    from e in Database.Query<SMSUpdatePackageEntity>()
                    let p = e.LastProcess()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        NumLines = e.SMSMessages().Count(),
                        LastProcess = p,
                        NumErrors = e.SMSMessages().Count(s => s.Exception(p) != null),
                    });
            }
        }

        private static ProcessEntity UpdateMessages(List<SMSMessageEntity> messages)
        {
            SMSUpdatePackageEntity package = new SMSUpdatePackageEntity().Save();

            var packLite = package.ToLite();

            if (messages.Any(m => m.State != SMSMessageState.Sent))
                throw new ApplicationException("SMS messages must be sent prior to update the status");

            messages.ForEach(ms => ms.UpdatePackage = packLite);
            messages.SaveList();

            var process = ProcessLogic.Create(SMSMessageProcess.UpdateStatus, package);

            process.Execute(ProcessOperation.Execute);

            return process;
        }



        #endregion
        
        public static void SendSMS(SMSMessageEntity message)
        {
            if (Provider == null)
                throw new InvalidOperationException("Provider was not established");

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
            message.MessageID = Provider.SMSSendAndGetTicket(message);
            message.SendDate = TimeZoneManager.Now.TrimToSeconds();
            message.State = SMSMessageState.Sent;
            using (OperationLogic.AllowSave<SMSMessageEntity>())
                message.Save();
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
            var IDs = Provider.SMSMultipleSendAction(template, phones);
            var sendDate = TimeZoneManager.Now.TrimToSeconds();
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
    }

    public class SMSMessageGraph : Graph<SMSMessageEntity, SMSMessageState>
    {
        public static void Register()
        {
            GetState = m => m.State;

            new ConstructFrom<SMSTemplateEntity>(SMSMessageOperation.CreateSMSFromSMSTemplate)
            {
                CanConstruct = t => !t.Active ? SMSCharactersMessage.TheTemplateMustBeActiveToConstructSMSMessages.NiceToString() : null,
                ToStates = { SMSMessageState.Created },
                Construct = (t, args) =>
                {
                    var defaultCulture = SMSLogic.Configuration.DefaultCulture.ToCultureInfo();
                    var ci = args.TryGetArgC<CultureInfo>() ?? defaultCulture;

                    return new SMSMessageEntity
                    {
                        Template = t.ToLite(),
                        Message = (t.GetCultureMessage(ci) ?? t.GetCultureMessage(defaultCulture)).Message,
                        From = t.From,
                        EditableMessage = t.EditableMessage,
                        State = SMSMessageState.Created,
                        DestinationNumber = args.TryGetArgC<string>(),
                        Certified = t.Certified
                    };
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
                        using (Transaction tr = Transaction.ForceNew())
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
                        func = SMSLogic.Provider.SMSUpdateStatusAction;

                    sms.State = func(sms);

                    if (sms.UpdatePackage != null)
                        sms.UpdatePackageProcessed = true;
                }
            }.Register();
        }
    }

    public interface ISMSProvider
    {
        string SMSSendAndGetTicket(SMSMessageEntity message);
        List<string> SMSMultipleSendAction(MultipleSMSModel template, List<string> phones);
        SMSMessageState SMSUpdateStatusAction(SMSMessageEntity message);
    }

  
}
