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
        static Func<SMSTemplateDN, List<string>, List<string>> SMSMultipleSendAction;
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
                                                 m.SourceNumber,
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

        public static void RegisterPhoneNumberProvider<T>(Expression<Func<T, string>> func)
        {
            phoneNumberProviders.Add(typeof(T), func);
        }

        public static string GetPhoneNumber(IdentifiableEntity entity)
        {
            return giGetPhoneNumber.GetInvoker(entity.GetType())(entity);
        }

        static GenericInvoker<Func<IdentifiableEntity, string>> giGetPhoneNumber = 
            new GenericInvoker<Func<IdentifiableEntity,string>>(ie => GetPhoneNumber<IdentifiableEntity>(ie));
        public static string GetPhoneNumber<T>(T entity)
        {
            return ((Expression<Func<T, string>>)phoneNumberProviders[typeof(T)]).Invoke(entity);
        }
        
        #region processes

        public static void StartProcesses(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                //TODO: 777 luis hay que hacer getoperationinfo para lanzar ex si no están registrados
                //SMSMessageGraph.Register();
                //SMSTemplateGraph.Register();

                sb.Include<SMSSendPackageDN>();
                SMSLogic.AssertStarted(sb);
                ProcessLogic.AssertStarted(sb);
                ProcessLogic.Register(SMSMessageProcess.Send, new SMSMessageSendProcessAlgortihm());
                ProcessLogic.Register(SMSMessageProcess.UpdateStatus, new SMSMessageUpdateStatusProcessAlgortihm());

                new BasicConstructFromMany<IdentifiableEntity, ProcessExecutionDN>(SMSMessageOperations.Send)
                {
                    Construct = (messages, args) => SendMessages(args.GetArg<SMSTemplateDN>(0), messages.RetrieveFromListOfLite())
                }.Register();

                new BasicConstructFromMany<SMSMessageDN, ProcessExecutionDN>(SMSMessageOperations.UpdateStatus) 
                {
                    Construct = (messages, _) => UpdateMessages(messages.RetrieveFromListOfLite())
                }.Register();

                //TODO: 777 luis - hay que registrar correctamente el proceso para todos los mensajes del sistema
                //new BasicExecute<ProcessExecutionDN>(SMSMessageOperations.UpdateStatus)
                //{
                //    Execute = (_, __) => new SMSMessageUpdateStatusProcessAlgortihm().CreateData()
                //}.Register();

                dqm[typeof(SMSSendPackageDN)] = (from e in Database.Query<SMSSendPackageDN>()
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
            SMSSendPackageDN package = new SMSSendPackageDN
            {
                NumLines = messages.Count,
            }.Save();

            var packLite = package.ToLite();

            if (messages.Any(m => m.State != SMSMessageState.Sent))
                throw new ApplicationException("SMS messages must be sent prior to update the status");

            messages.Select(m => m.Do(ms => ms.Package = packLite)).SaveList();

            var process = ProcessLogic.Create(SMSMessageProcess.Send, package);

            process.ToLite().ExecuteLite(ProcessOperation.Execute);

            return process;
        }

        public static ProcessExecutionDN SendMessages<T>(SMSTemplateDN template, List<T> recipientList)
            where T : class, IIdentifiable
        {
            SMSSendPackageDN package = new SMSSendPackageDN
            {
                NumLines = recipientList.Count,
            }.Save();

            var packLite = package.ToLite();

            recipientList.Select(e => template.CreateSMSMessage(SMSLogic.GetPhoneNumber(e), packLite)).SaveList();

            var process = ProcessLogic.Create(SMSMessageProcess.Send, package);

            process.ToLite().ExecuteLite(ProcessOperation.Execute);

            return process;
        }

        #endregion

        public static void RegisterSMSSendAction(Func<SMSMessageDN, string> action)
        {
            SMSSendAndGetTicketAction = action;
        }

        public static void RegisterSMSMultipleSendAction(Func<SMSTemplateDN, List<string>, List<string>> action)
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
            message.State = SMSMessageState.Sent;
            message.Save();
        }

        public static List<SMSMessageDN> CreateAndSendMultipleSMSMessages(SMSTemplateDN template, List<string> phones)
        {
            return CreateAndSendMultipleSMSMessages(template, phones, SMSMultipleSendAction);
        }

        //Allows concurrent custom sendProviders for one application
        public static List<SMSMessageDN> CreateAndSendMultipleSMSMessages(SMSTemplateDN template,
            List<string> phones, Func<SMSTemplateDN, List<string>, List<string>> send)
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
        public static void Register()
        {
            GetState = m => m.State;

            new ConstructFrom<SMSTemplateDN>(SMSMessageOperations.Create)
            {
                CanConstruct = t => !t.Active ? "The template must be Active to allow constructing SMS messages" : null,
                ToState = SMSMessageState.Created,
                Construct = (t, args) =>
                {
                    var message = t.CreateSMSMessage();
                    message.DestinationNumber = args.GetArg<string>(0);
                    return t.CreateSMSMessage();
                }
            }.Register();

            new Execute(SMSMessageOperations.Send)
            {
                FromStates = new[] { SMSMessageState.Created },
                ToState = SMSMessageState.Sent,
                Execute = (t, args) => { SMSLogic.SendSMS(t, args.TryGetArgC<Func<SMSMessageDN, string>>(0)); }
            }.Register();

            new Execute(SMSMessageOperations.UpdateStatus)
            {
                FromStates = new[] { SMSMessageState.Sent },
                ToState = SMSMessageState.Sent,
                Execute = (t, args) => { SMSLogic.UpdateMessageStatus(t, args.TryGetArgC<Func<SMSMessageDN, SendState>>(0)); }
            }.Register();
        }
    }

}
