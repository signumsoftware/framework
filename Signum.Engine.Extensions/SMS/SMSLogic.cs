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

namespace Signum.Engine.SMS
{
    public static class SMSLogic
    {
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

        static Func<SMSMessageDN, string> SMSSendAndGetTicketAction;
        static Func<CreateMessageParams, List<string>, List<string>> SMSMultipleSendAction;
        static Func<SMSMessageDN, SMSMessageState> SMSUpdateStatusAction;

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null, null, false)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, bool registerGraph)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<SMSMessageDN>();

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
                        Message = t.Message.Etc(50),
                        Source = t.From,
                        t.AssociatedType,
                        t.State,
                        t.StartDate,
                        t.EndDate,
                    });

                if (registerGraph)
                {
                    SMSMessageGraph.Register();
                    SMSTemplateGraph.Register();
                }
            }
        }

        static Dictionary<Type, LambdaExpression> phoneNumberProviders = new Dictionary<Type, LambdaExpression>();

        public static void RegisterPhoneNumberProvider<T>(Expression<Func<T, string>> exp) where T : IdentifiableEntity
        {
            phoneNumberProviders[typeof(T)] = exp;

            new Graph<ProcessDN>.ConstructFromMany<T>(SMSProviderOperation.SendSMSMessage)
            {
                Construct = (providers, args) =>
                {
                    var numbers = Database.Query<T>().Where(p => providers.Contains(p.ToLite()))
                        .Select(pr => new { Exp = exp.Evaluate(pr), Referred = pr.ToLite() }).AsEnumerable().NotNull().Distinct().ToList();

                    CreateMessageParams createParams = args.GetArg<CreateMessageParams>();

                    if (!createParams.Message.HasText())
                        throw new ApplicationException("The text for the SMS message has not been set");

                    SMSSendPackageDN package = new SMSSendPackageDN().Save();

                    var packLite = package.ToLite();

                    using (OperationLogic.AllowSave<SMSMessageDN>())
                        numbers.Select(n => createParams.CreateStaticSMSMessage(n.Exp, packLite, n.Referred, createParams.Certified)).SaveList();

                    var process = ProcessLogic.Create(SMSMessageProcess.Send, package);

                    process.Execute(ProcessOperation.Execute);

                    return process;
                }
            }.Register();
        }

        [Serializable]
        public class CreateMessageParams
        {
            public string Message;
            public string From;
            public bool Certified;
            public List<Lite<IdentifiableEntity>> Referreds;

            public SMSMessageDN CreateStaticSMSMessage(string destinationNumber, Lite<SMSSendPackageDN> packLite, Lite<IdentifiableEntity> referred, bool certified)
            {
                return new SMSMessageDN
                {
                    Message = this.Message,
                    From = this.From,
                    State = SMSMessageState.Created,
                    DestinationNumber = destinationNumber,
                    SendPackage = packLite,
                    Referred = referred,
                    Certified = certified
                };
            }
        }

        public static string GetPhoneNumber<T>(T entity) where T : IIdentifiable
        {
            var phoneFunc = (Expression<Func<T, string>>)phoneNumberProviders.
                       GetOrThrow(typeof(T), "{0} is not registered as PhoneNumberProvider");

            return phoneFunc.Evaluate(entity);
        }

        #region Message composition

        static Dictionary<Type, LambdaExpression> dataObjectProviders = new Dictionary<Type, LambdaExpression>();

        public static List<Lite<TypeDN>> RegisteredDataObjectProviders()
        {
            return dataObjectProviders.Keys.Select(t => TypeLogic.ToTypeDN(t).ToLite()).ToList();
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

            new Graph<ProcessDN>.ConstructFromMany<T>(SMSProviderOperation.SendSMSMessagesFromTemplate)
            {
                Construct = (providers, args) =>
                {
                    var template = args.GetArg<SMSTemplateDN>();

                    if (TypeLogic.DnToType[template.AssociatedType] != typeof(T))
                        throw new ArgumentException("The SMS template is associated with the type {0} instead of {1}"
                            .Formato(template.AssociatedType.FullClassName, typeof(T).FullName));

                    var phoneFunc = (Expression<Func<T, string>>)phoneNumberProviders.
                        GetOrThrow(typeof(T), "{0} is not registered as PhoneNumberProvider");

                    var numbers = Database.Query<T>().Where(p => providers.Contains(p.ToLite()))
                          .Select(p => new
                          {
                              Phone = phoneFunc.Evaluate(p),
                              Data = func.Evaluate(p),
                              Referred = p.ToLite()
                          }).Where(n => n.Phone.HasText()).AsEnumerable().ToList();

                    SMSSendPackageDN package = new SMSSendPackageDN().Save();
                    var packLite = package.ToLite();

                    numbers.Select(n => new SMSMessageDN
                    {
                        Message = template.ComposeMessage(n.Data),
                        EditableMessage = template.EditableMessage,
                        From = template.From,
                        DestinationNumber = n.Phone,
                        SendPackage = packLite,
                        State = SMSMessageState.Created,
                        Referred = n.Referred
                    }).SaveList();

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
                        Message = template.ComposeMessage(func.Evaluate(provider)),
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

        static string ComposeMessage(this SMSTemplateDN template, object o)
        {
            if (o == null)
                return template.Message;

            var matches = literalFinder.Matches(template.Message);

            if (matches.Count == 0)
                return template.Message;

            Type t = o.GetType();

            var combinations = (from Match m in literalFinder.Matches(template.Message)
                                select new Combination
                                {
                                    Name = m.Groups["name"].Value,
                                    Value = t.GetProperty(m.Groups["name"].Value).TryCC(fi => fi.GetValue(o, null)).TryToString()
                                }).ToList();

            return CombineText(template, combinations);
        }

        internal class Combination
        {
            public string Name;
            public string Value;
        }

        static string CombineText(SMSTemplateDN template, List<Combination> combinations)
        {
            string text = template.Message;
            if (template.RemoveNoSMSCharacters)
            {
                text = SMSCharacters.RemoveNoSMSCharacters(template.Message);
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
                if (!SMSMessageGraph.Registered)
                    throw new InvalidOperationException("SMSMessageGraph must be registered prior to start the processes");

                if (!SMSTemplateGraph.Registered)
                    throw new InvalidOperationException("SMSTemplateGraph must be registered prior to start the processes");

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
                    let p  = e.LastProcess()
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

        public static void RegisterSMSSendAction(Func<SMSMessageDN, string> action)
        {
            SMSSendAndGetTicketAction = action;
        }

        public static void RegisterSMSMultipleSendAction(Func<CreateMessageParams, List<string>, List<string>> action)
        {
            SMSMultipleSendAction = action;
        }

        public static void RegisterSMSUpdateStatusAction(Func<SMSMessageDN, SMSMessageState> action)
        {
            SMSUpdateStatusAction = action;
        }

        public static void SendSMS(SMSMessageDN message)
        {
            if (SMSSendAndGetTicketAction == null)
                throw new InvalidOperationException("SMSSendAction was not established");

            SendSMS(message, SMSSendAndGetTicketAction);
        }

        //Allows concurrent custom sendProviders for one application
        public static void SendSMS(SMSMessageDN message, Func<SMSMessageDN, string> sendAndGetTicket)
        {
            message.MessageID = sendAndGetTicket(message);
            message.SendDate = TimeZoneManager.Now.TrimToSeconds();
            message.State = SMSMessageState.Sent;
            message.Save();
        }

        public static List<SMSMessageDN> CreateAndSendMultipleSMSMessages(CreateMessageParams template, List<string> phones)
        {
            return CreateAndSendMultipleSMSMessages(template, phones, SMSMultipleSendAction);
        }

        //Allows concurrent custom sendProviders for one application
        public static List<SMSMessageDN> CreateAndSendMultipleSMSMessages(CreateMessageParams template,
            List<string> phones, Func<CreateMessageParams, List<string>, List<string>> send)
        {
            var messages = new List<SMSMessageDN>();
            var IDs = send(template, phones);
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

        public static void UpdateMessageStatus(SMSMessageDN message)
        {
            if (SMSUpdateStatusAction == null)
                throw new InvalidOperationException("SMSUpdateStatusAction was not established");

            UpdateMessageStatus(message, SMSUpdateStatusAction);
        }

        //Allows concurrent custom updateStatusProviders for one application
        public static void UpdateMessageStatus(SMSMessageDN message, Func<SMSMessageDN, SMSMessageState> updateAction)
        {
            message.State = updateAction(message);
        }
    }

    public class SMSMessageGraph : Graph<SMSMessageDN, SMSMessageState>
    {
        static bool registered;
        public static bool Registered { get { return registered; } }

        public static void Register()
        {
            GetState = m => m.State;

            new ConstructFrom<SMSTemplateDN>(SMSMessageOperation.CreateSMSFromSMSTemplate)
            {
                CanConstruct = t => !t.Active ? SmsMessage.TheTemplateMustBeActiveToConstructSMSMessages.NiceToString() : null,
                ToState = SMSMessageState.Created,
                Construct = (t, args) =>
                {
                    var message = t.CreateStaticSMSMessage();
                    message.DestinationNumber = args.TryGetArgC<string>();
                    return message;
                }
            }.Register();

            new Execute(SMSMessageOperation.Send)
            {
                AllowsNew = true,
                Lite = false,
                FromStates = { SMSMessageState.Created },
                ToState = SMSMessageState.Sent,
                Execute = (t, args) =>
                {
                    var func = args.TryGetArgC<Func<SMSMessageDN, string>>();
                    if (func != null)
                        SMSLogic.SendSMS(t, func);
                    else
                        SMSLogic.SendSMS(t);
                }
            }.Register();

            new Graph<SMSMessageDN>.Execute(SMSMessageOperation.UpdateStatus)
            {
                CanExecute = m => m.State != SMSMessageState.Created ? null : SmsMessage.StatusCanNotBeUpdatedForNonSentMessages.NiceToString(),
                Execute = (sms, args) =>
                {
                    var func = args.TryGetArgC<Func<SMSMessageDN, SMSMessageState>>();
                    if (func != null)
                        SMSLogic.UpdateMessageStatus(sms, func);
                    else
                        SMSLogic.UpdateMessageStatus(sms);

                    //if (sms.State == SMSMessageState.Sent)
                    //    throw new InvalidOperationException("SMS Message {0} has not updated state".Formato(sms.Id));
                }
            }.Register();

            registered = true;
        }
    }

}
