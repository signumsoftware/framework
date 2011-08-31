using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Entities.SMS;
using Signum.Engine.Extensions.Properties;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Engine.Operations;
using Signum.Engine.Processes;
using Signum.Entities.Processes;
using Signum.Engine.Extensions.SMS;
using System.Linq.Expressions;

namespace Signum.Engine.SMS
{
    public static class SMSLogic
    {
        static Func<SMSMessageDN, string> SMSSendAndGetTicketAction;
        static Func<CreateMessageParams, List<string>, List<string>> SMSMultipleSendAction;
        static Func<SMSMessageDN, SendState> SMSUpdateStatusAction;

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null, null, false)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, bool registerGraph)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<SMSMessageDN>();

                dqm[typeof(SMSMessageDN)] = (from m in Database.Query<SMSMessageDN>()
                                             select new
                                             {
                                                 Entity = m.ToLite(),
                                                 m.Id,
                                                 Source = m.From,
                                                 m.DestinationNumber,
                                                 m.State,
                                                 m.SendDate,
                                                 m.Template
                                             }).ToDynamic();

                dqm[typeof(SMSTemplateDN)] = (from t in Database.Query<SMSTemplateDN>()
                                              select new
                                              {
                                                  Entity = t.ToLite(),
                                                  t.Id,
                                                  t.Name,
                                                  IsActive = t.IsActiveNow(),
                                                  Message = t.Message.Etc(20),
                                                  Source = t.From,
                                                  t.State,
                                                  t.StartDate,
                                                  t.EndDate,
                                              }).ToDynamic();

                if (registerGraph)
                {
                    SMSMessageGraph.Register();
                    SMSTemplateGraph.Register();
                }
            }
        }

        static Dictionary<Type, LambdaExpression> phoneNumberProviders = new Dictionary<Type, LambdaExpression>();


        public static void RegisterPhoneNumberProvider<T>(Expression<Func<T, string>> func) where T : IdentifiableEntity
        {
            phoneNumberProviders.Add(typeof(T), func);

            new BasicConstructFromMany<T, ProcessExecutionDN>(SMSProviderOperations.SendSMSMessage)
            {
                Construct = (providers, args) =>
                {
                    var numbers = Database.Query<T>().Where(p => providers.Contains(p.ToLite()))
                        .Select(func).AsEnumerable().NotNull().Distinct().ToList();

                    CreateMessageParams createParams = args.GetArg<CreateMessageParams>(0);

                    if (!createParams.Message.HasText())
                        throw new ApplicationException("The text for the SMS message has not been set");

                    SMSPackageDN package = new SMSPackageDN
                    {
                        NumLines = numbers.Count,
                    }.Save();

                    var packLite = package.ToLite();

                    numbers.Select(n => createParams.CreateSMSMessage(n, packLite)).SaveList();

                    var process = ProcessLogic.Create(SMSMessageProcess.Send, package);

                    process.ToLite().ExecuteLite(ProcessOperation.Execute);

                    return process;
                }
            }.Register();
        }

        public class CreateMessageParams
        {
            public string Message;
            public string From;

            public SMSMessageDN CreateSMSMessage(string destinationNumber, Lite<SMSPackageDN> packLite)
            {
                return new SMSMessageDN
                {
                    Message = this.Message,
                    From = this.From,
                    State = SMSMessageState.Created,
                    DestinationNumber = destinationNumber,
                    SendPackage = packLite
                };
            }

            public SMSMessageDN CreateSMSMessage()
            {
                return CreateSMSMessage(null);
            }

            public SMSMessageDN CreateSMSMessage(string destinationNumber)
            {
                return new SMSMessageDN
                {
                    Message = this.Message,
                    From = this.From,
                    State = SMSMessageState.Created,
                    DestinationNumber = destinationNumber
                };
            }

        }

        public static string GetPhoneNumber<T>(T entity) where T : IIdentifiable
        {
            return ((Expression<Func<T, string>>)phoneNumberProviders[typeof(T)]).Invoke(entity);
        }

        #region processes

        public static void StartProcesses(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (!SMSMessageGraph.Registered)
                    throw new InvalidOperationException("SMSMessageGraph must be registered prior to start the processes");

                if (!SMSTemplateGraph.Registered)
                    throw new InvalidOperationException("SMSTemplateGraph must be registered prior to start the processes");

                sb.Include<SMSPackageDN>();
                SMSLogic.AssertStarted(sb);
                ProcessLogic.AssertStarted(sb);
                ProcessLogic.Register(SMSMessageProcess.Send, new SMSMessageSendProcessAlgortihm());
                ProcessLogic.Register(SMSMessageProcess.UpdateStatus, new SMSMessageUpdateStatusProcessAlgorithm());

                new BasicConstructFromMany<SMSMessageDN, ProcessExecutionDN>(SMSMessageOperations.CreateUpdateStatusPackage)
                {
                    Construct = (messages, _) => UpdateMessages(messages.RetrieveFromListOfLite())
                }.Register();

                dqm[typeof(SMSPackageDN)] = (from e in Database.Query<SMSPackageDN>()
                                             select new
                                             {
                                                 Entity = e.ToLite(),
                                                 e.Id,
                                                 e.Error,
                                                 e.NumLines,
                                                 e.NumErrors,
                                             }).ToDynamic();
            }
        }

        private static ProcessExecutionDN UpdateMessages(List<SMSMessageDN> messages)
        {
            SMSPackageDN package = new SMSPackageDN
            {
                NumLines = messages.Count,
            }.Save();

            var packLite = package.ToLite();

            if (messages.Any(m => m.State != SMSMessageState.Sent))
                throw new ApplicationException("SMS messages must be sent prior to update the status");

            messages.Select(m => m.Do(ms => ms.SendPackage = packLite)).SaveList();

            var process = ProcessLogic.Create(SMSMessageProcess.Send, package);

            process.ToLite().ExecuteLite(ProcessOperation.Execute);

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

        public static void RegisterSMSUpdateStatusAction(Func<SMSMessageDN, SendState> action)
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
            message.SendDate = DateTime.Now.TrimToSeconds();
            message.SendState = SendState.Sent;
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
            var sendDate = DateTime.Now.TrimToSeconds();
            for (int i = 0; i < phones.Count; i++)
            {
                var message = template.CreateSMSMessage();
                message.SendDate = sendDate;
                message.SendState = SendState.Sent;
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
        public static void UpdateMessageStatus(SMSMessageDN message, Func<SMSMessageDN, SendState> updateAction)
        {
            message.SendState = updateAction(message);
        }

    }

    public class SMSMessageGraph : Graph<SMSMessageDN, SMSMessageState>
    {
        static bool registered;
        public static bool Registered { get { return registered; } }

        public static void Register()
        {
            GetState = m => m.State;

            new ConstructFrom<SMSTemplateDN>(SMSMessageOperations.Create)
            {
                CanConstruct = t => !t.Active ? Resources.TheTemplateMustBeActiveToConstructSMSMessages : null,
                ToState = SMSMessageState.Created,
                Construct = (t, args) =>
                {
                    var message = t.CreateSMSMessage();
                    message.DestinationNumber = args.TryGetArgC<string>(0);
                    return message;
                }
            }.Register();

            new Execute(SMSMessageOperations.Send)
            {
                AllowsNew = true,
                Lite = false,
                FromStates = new[] { SMSMessageState.Created },
                ToState = SMSMessageState.Sent,
                Execute = (t, args) =>
                {
                    var func = args.TryGetArgC<Func<SMSMessageDN, string>>(0);
                    if (func != null)
                        SMSLogic.SendSMS(t, func);
                    else
                        SMSLogic.SendSMS(t);
                }
            }.Register();

            new Execute(SMSMessageOperations.UpdateStatus)
            {
                FromStates = new[] { SMSMessageState.Sent },
                ToState = SMSMessageState.Sent,
                Execute = (t, args) => 
                {
                    var func = args.TryGetArgC<Func<SMSMessageDN, SendState>>(0);
                    if (func != null)
                        SMSLogic.UpdateMessageStatus(t, func);
                    else
                        SMSLogic.UpdateMessageStatus(t);
                }
            }.Register();

            registered = true;
        }
    }

}
