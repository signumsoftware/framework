using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Signum.Utilities.ExpressionTrees;
using System.Threading.Tasks;
using System.Globalization;
using Signum.Entities.Translation;
using Signum.Engine.Translation;

namespace Signum.Engine.SMS
{

    public static class SMSLogic
    {
        public static SMSTemplateMessageDN GetCultureMessage(this SMSTemplateDN template, CultureInfo ci)
        {
            return template.Messages.SingleOrDefault(tm => tm.CultureInfo.ToCultureInfo() == ci);
        }

        public static Expression<Func<IdentifiableEntity, IQueryable<SMSMessageDN>>> SMSMessagesExpression =
            e => Database.Query<SMSMessageDN>().Where(m => m.Referred.RefersTo(e));
        public static IQueryable<SMSMessageDN> SMSMessages(this IdentifiableEntity e)
        {
            return SMSMessagesExpression.Evaluate(e);
        }

        static Expression<Func<SMSSendPackageDN, IQueryable<SMSMessageDN>>> SMSMessagesSendExpression =
            e => Database.Query<SMSMessageDN>().Where(a => a.SendPackage.RefersTo(e));
        [ExpressionField("SMSMessagesSendExpression")]
        public static IQueryable<SMSMessageDN> SMSMessages(this SMSSendPackageDN e)
        {
            return SMSMessagesSendExpression.Evaluate(e);
        }

        static Expression<Func<SMSUpdatePackageDN, IQueryable<SMSMessageDN>>> SMSMessagesUpdateExpression =
          e => Database.Query<SMSMessageDN>().Where(a => a.UpdatePackage.RefersTo(e));
        [ExpressionField("SMSMessagesUpdateExpression")]
        public static IQueryable<SMSMessageDN> SMSMessages(this SMSUpdatePackageDN e)
        {
            return SMSMessagesUpdateExpression.Evaluate(e);
        }



        static Func<SMSConfigurationDN> getConfiguration;
        public static SMSConfigurationDN Configuration
        {
            get { return getConfiguration(); }
        }

        public static ISMSProvider Provider { get; set; }

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null, null, null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, ISMSProvider provider, Func<SMSConfigurationDN> getConfiguration)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                CultureInfoLogic.AssertStarted(sb);

                SMSLogic.getConfiguration = getConfiguration;
                SMSLogic.Provider = provider;

                sb.Include<SMSMessageDN>();
                sb.Include<SMSTemplateDN>();

                dqm.RegisterQuery(typeof(SMSMessageDN), () =>
                    from m in Database.Query<SMSMessageDN>()
                    select new
                    {
                        Entity = m,
                        m.Id,
                        Source = m.From,
                        m.DestinationNumber,
                        m.State,
                        m.SendDate,
                        m.Template
                    });

                dqm.RegisterQuery(typeof(SMSTemplateDN), () =>
                    from t in Database.Query<SMSTemplateDN>()
                    select new
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

                Validator.PropertyValidator<SMSTemplateDN>(et => et.Messages).StaticPropertyValidation += (t, pi) =>
                {
                    if (!t.Messages.Any(m => m.CultureInfo.Is(SMSLogic.Configuration.DefaultCulture)))
                        return SMSTemplateMessage.ThereMustBeAMessageFor0.NiceToString().Formato(SMSLogic.Configuration.DefaultCulture.EnglishName);

                    return null;
                };
            }
        }

        static Dictionary<Type, LambdaExpression> phoneNumberProviders = new Dictionary<Type, LambdaExpression>();
        static Dictionary<Type, LambdaExpression> cultureProviders = new Dictionary<Type, LambdaExpression>();

        public static void RegisterPhoneNumberProvider<T>(Expression<Func<T, string>> phoneExpression, Expression<Func<T, CultureInfo>> cultureExpression) where T : IdentifiableEntity
        {
            phoneNumberProviders[typeof(T)] = phoneExpression;
            cultureProviders[typeof(T)] = cultureExpression;

            new Graph<ProcessDN>.ConstructFromMany<T>(SMSMessageOperation.SendSMSMessages)
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

                    string str = model.IntegrityCheck();

                    if (!model.Message.HasText())
                        throw new ApplicationException("The text for the SMS message has not been set");

                    SMSSendPackageDN package = new SMSSendPackageDN().Save();

                    var packLite = package.ToLite();

                    using (OperationLogic.AllowSave<SMSMessageDN>())
                        numbers.Select(n => new SMSMessageDN
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



        public static string GetPhoneNumber<T>(T entity) where T : IIdentifiable
        {
            var phoneFunc = (Expression<Func<T, string>>)phoneNumberProviders
                .GetOrThrow(typeof(T), "{0} is not registered as PhoneNumberProvider".Formato(typeof(T).NiceName()));

            return phoneFunc.Evaluate(entity);
        }

        public static CultureInfo GetCulture<T>(T entity) where T : IIdentifiable
        {
            var cultureFunc = (Expression<Func<T, CultureInfo>>)cultureProviders
                .GetOrThrow(typeof(T), "{0} is not registered as CultureProvider".Formato(typeof(T).NiceName()));

            return cultureFunc.Evaluate(entity);
        }

        #region Message composition

        static Dictionary<Type, LambdaExpression> dataObjectProviders = new Dictionary<Type, LambdaExpression>();

        public static List<Lite<TypeDN>> RegisteredDataObjectProviders()
        {
            return dataObjectProviders.Keys.Select(t => t.ToTypeDN().ToLite()).ToList();
        }

        public static List<string> GetLiteralsFromDataObjectProvider(Type type)
        {
            if (!dataObjectProviders.ContainsKey(type))
                throw new ArgumentOutOfRangeException("The type {0} is not a registered data provider"
                    .Formato(type.FullName));

            return dataObjectProviders[type].GetType().GetGenericArguments()[0]
                .GetGenericArguments()[1].GetProperties().Select(p => "{{{0}}}".Formato(p.Name)).ToList();
        }

        public static void RegisterDataObjectProvider<T, A>(Expression<Func<T, A>> func) where T : IdentifiableEntity
        {
            dataObjectProviders[typeof(T)] = func;

            new Graph<ProcessDN>.ConstructFromMany<T>(SMSMessageOperation.SendSMSMessagesFromTemplate)
            {
                Construct = (providers, args) =>
                {
                    var template = args.GetArg<SMSTemplateDN>();

                    if (TypeLogic.DnToType[template.AssociatedType] != typeof(T))
                        throw new ArgumentException("The SMS template is associated with the type {0} instead of {1}"
                            .Formato(template.AssociatedType.FullClassName, typeof(T).FullName));

                    var phoneFunc = (Expression<Func<T, string>>)phoneNumberProviders
                        .GetOrThrow(typeof(T), "{0} is not registered as PhoneNumberProvider".Formato(typeof(T).NiceName()));

                    var cultureFunc = (Expression<Func<T, CultureInfo>>)cultureProviders
                        .GetOrThrow(typeof(T), "{0} is not registered as CultureProvider".Formato(typeof(T).NiceName()));

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

                    SMSSendPackageDN package = new SMSSendPackageDN().Save();
                    var packLite = package.ToLite();

                    using (OperationLogic.AllowSave<SMSMessageDN>())
                    {
                        numbers.Select(n => new SMSMessageDN
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

            new Graph<SMSMessageDN>.ConstructFrom<T>(SMSMessageOperation.CreateSMSWithTemplateFromEntity)
            {
                Construct = (provider, args) =>
                {
                    var template = args.GetArg<SMSTemplateDN>();

                    if (template.AssociatedType != null &&
                        TypeLogic.DnToType[template.AssociatedType] != typeof(T))
                        throw new ArgumentException("The SMS template is associated with the type {0} instead of {1}"
                            .Formato(template.AssociatedType.FullClassName, typeof(T).FullName));

                    return new SMSMessageDN
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

        public static string ComposeMessage(this SMSTemplateDN template, object o, CultureInfo culture)
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
                                    Value = t.GetProperty(m.Groups["name"].Value).Try(fi => fi.GetValue(o, null)).TryToString()
                                }).ToList();

            return CombineText(template, templateMessage, combinations);
        }

        internal class Combination
        {
            public string Name;
            public string Value;
        }

        static string CombineText(SMSTemplateDN template, SMSTemplateMessageDN templateMessage, List<Combination> combinations)
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
                        throw new ApplicationException(SmsMessage.TheTextForTheSMSMessageExceedsTheLengthLimit.NiceToString());
                    case MessageLengthExceeded.Allowed:
                        break;
                    case MessageLengthExceeded.TextPruning:
                        return result.RemoveEnd(Math.Abs(remainingLength));
                }
            }

            return result;
        }


        internal class CombinedLiteral
        {
            public string Name;
            public string Value;
        }


        #endregion



        #region processes

        public static void StartProcesses(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<SMSSendPackageDN>();
                sb.Include<SMSUpdatePackageDN>();
                SMSLogic.AssertStarted(sb);
                ProcessLogic.AssertStarted(sb);
                ProcessLogic.Register(SMSMessageProcess.Send, new SMSMessageSendProcessAlgortihm());
                ProcessLogic.Register(SMSMessageProcess.UpdateStatus, new SMSMessageUpdateStatusProcessAlgorithm());

                new Graph<ProcessDN>.ConstructFromMany<SMSMessageDN>(SMSMessageOperation.CreateUpdateStatusPackage)
                {
                    Construct = (messages, _) => UpdateMessages(messages.RetrieveFromListOfLite())
                }.Register();

                dqm.RegisterQuery(typeof(SMSSendPackageDN), () =>
                    from e in Database.Query<SMSSendPackageDN>()
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

                dqm.RegisterQuery(typeof(SMSUpdatePackageDN), () =>
                    from e in Database.Query<SMSUpdatePackageDN>()
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

        private static ProcessDN UpdateMessages(List<SMSMessageDN> messages)
        {
            SMSUpdatePackageDN package = new SMSUpdatePackageDN().Save();

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

      

        public static void SendSMS(SMSMessageDN message)
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
                    SendOneMessage(new SMSMessageDN
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

        private static void SendOneMessage(SMSMessageDN message)
        {
            message.MessageID = Provider.SMSSendAndGetTicket(message);
            message.SendDate = TimeZoneManager.Now.TrimToSeconds();
            message.State = SMSMessageState.Sent;
            message.Save();
        }

        public static void SendAsyncSMS(SMSMessageDN message)
        {
            Task.Factory.StartNew(() =>
            {
                SendSMS(message);
            });
        }

        public static List<SMSMessageDN> CreateAndSendMultipleSMSMessages(MultipleSMSModel template, List<string> phones)
        {
            var messages = new List<SMSMessageDN>();
            var IDs = Provider.SMSMultipleSendAction(template, phones);
            var sendDate = TimeZoneManager.Now.TrimToSeconds();
            for (int i = 0; i < phones.Count; i++)
            {
                var message = new SMSMessageDN { Message = template.Message, From = template.From };
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

    public class SMSMessageGraph : Graph<SMSMessageDN, SMSMessageState>
    {
        public static void Register()
        {
            GetState = m => m.State;

            new ConstructFrom<SMSTemplateDN>(SMSMessageOperation.CreateSMSFromSMSTemplate)
            {
                CanConstruct = t => !t.Active ? SmsMessage.TheTemplateMustBeActiveToConstructSMSMessages.NiceToString() : null,
                ToState = SMSMessageState.Created,
                Construct = (t, args) =>
                {
                    var defaultCulture = SMSLogic.Configuration.DefaultCulture.ToCultureInfo();
                    var ci = args.TryGetArgC<CultureInfo>() ?? defaultCulture;

                    return new SMSMessageDN
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
                AllowsNew = true,
                Lite = false,
                FromStates = { SMSMessageState.Created },
                ToState = SMSMessageState.Sent,
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

            new Graph<SMSMessageDN>.Execute(SMSMessageOperation.UpdateStatus)
            {
                CanExecute = m => m.State != SMSMessageState.Created ? null : SmsMessage.StatusCanNotBeUpdatedForNonSentMessages.NiceToString(),
                Execute = (sms, args) =>
                {
                    var func = args.TryGetArgC<Func<SMSMessageDN, SMSMessageState>>();
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
        string SMSSendAndGetTicket(SMSMessageDN message);
        List<string> SMSMultipleSendAction(MultipleSMSModel template, List<string> phones);
        SMSMessageState SMSUpdateStatusAction(SMSMessageDN message);
    }

  
}
