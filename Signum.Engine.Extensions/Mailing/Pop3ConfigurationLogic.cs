using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Mailing.Pop3;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Engine.Scheduler;
using Signum.Entities;
using Signum.Entities.Mailing;
using Signum.Utilities;

namespace Signum.Engine.Mailing
{
    public static class Pop3ConfigurationLogic
    {
        public static WarningHandler Warning;
        public static TraceHandler Trace;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Settings.AssertNotIgnored((EmailMessageDN em) => em.Reception, "start Pop3ConfigurationLogic");
               
                sb.Include<Pop3ConfigurationDN>();
                sb.Include<Pop3ReceptionDN>();

                dqm.RegisterQuery(typeof(Pop3ReceptionDN), () =>
                    from s in Database.Query<Pop3ReceptionDN>()
                    select new
                    {
                        Entity = s,
                        s.Id,
                        s.Pop3Configuration,
                        s.StartDate,
                        s.EndDate,
                        s.NumberOfMails,
                        s.MailboxSize,
                        s.Exception
                    });

                dqm.RegisterQuery(typeof(Pop3ConfigurationDN), () =>
                    from s in Database.Query<Pop3ConfigurationDN>()
                    select new
                    {
                        Entity = s,
                        s.Id,
                        s.Name,
                        s.Host,
                        s.Port,
                        s.Username,
                        s.Password,
                        s.EnableSSL
                    });

                new Graph<Pop3ConfigurationDN>.Execute(Pop3ConfigurationOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { }
                }.Register();

                new Graph<Pop3ConfigurationDN>.Execute(Pop3ConfigurationOperation.ReceiveEmails)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => e.ReceiveEmails()
                }.Register();

                SchedulerLogic.ExecuteTask.Register((Pop3ConfigurationDN smtp) => smtp.ReceiveEmails().ToLite());

                SimpleTaskLogic.Register(Pop3ConfigurationAction.ReceiveAllActivePop3Configurations, () =>
                {
                    foreach (var item in Database.Query<Pop3ConfigurationDN>().Where(a=>a.Active).ToList())
                    {
                        item.ReceiveEmails();
                    }

                    return null;
                }); 
            }
        }

        public static Pop3ReceptionDN ReceiveEmails(this Pop3ConfigurationDN config)
        {
            Pop3ReceptionDN reception = new Pop3ReceptionDN { Pop3Configuration = config.ToLite(), StartDate = TimeZoneManager.Now };
            reception.Save();

            try
            {
                Pop3MimeClient client = new Pop3MimeClient(config.Host, config.Port, config.EnableSSL, config.Username, config.Password) { ReadTimeout = config.ReadTimeout };

                if (Warning != null)
                    client.Warning += Warning;

                if (Trace != null)
                    client.Trace += Trace;

                try
                {
                    client.Connect();

                    int numberOfMails;
                    int mailboxSize;
                    if (!client.GetMailboxStats(out numberOfMails, out mailboxSize))
                        throw new Pop3Exception("Error retrieving mailbox stats");

                    reception.NumberOfMails = numberOfMails;
                    reception.MailboxSize = mailboxSize;

                    int maxids = Math.Min(numberOfMails, config.MaxDownloadEmails);

                    for (int i = 1; i <= maxids; i++)
                    {
                        using (Transaction tr = Transaction.ForceNew())
                        {
                            try
                            {
                                RxMailMessage mm;
                                if (client.GetEmail(i, out mm))
                                {
                                    var email = ToEmailMessage(mm);
                                    email.Reception = reception.ToLite();

                                    if (AssociateWithEntities != null)
                                        AssociateWithEntities(email);


                                    using (OperationLogic.AllowSave<EmailMessageDN>())
                                        email.Save();

                                    if (config.DeleteAfterReception)
                                        client.DeleteEmail(i);
                                }
                            }
                            catch (Exception e)
                            {
                                e.LogException();
                            }

                            tr.Commit();
                        }
                    }

                    reception.EndDate = TimeZoneManager.Now;
                    reception.Save();
                }
                finally
                {
                    client.Disconnect();
                }
            }
            catch (Exception e)
            {

                var ex = e.LogException();

                try
                {
                    reception.EndDate = TimeZoneManager.Now;
                    reception.Exception = ex.ToLite();
                    reception.Save();
                }
                catch { }
            }

            return reception;
        }

        public static Action<EmailMessageDN> AssociateWithEntities;

        private static EmailMessageDN ToEmailMessage(RxMailMessage mm)
        {
            mm.MailStructure();

            return new EmailMessageDN
            {
                EditableMessage = false,
                From = new EmailAddressDN(mm.From),
                Recipients =
                   mm.To.Select(ma => new EmailRecipientDN(ma, EmailRecipientKind.To)).Concat(
                   mm.CC.Select(ma => new EmailRecipientDN(ma, EmailRecipientKind.CC))).Concat(
                   mm.Bcc.Select(ma => new EmailRecipientDN(ma, EmailRecipientKind.Bcc))).ToMList(),
                State = EmailMessageState.Received,
                IsBodyHtml = mm.IsBodyHtml,
                Subject = mm.Subject,
                Body = mm.Body,
                Received = TimeZoneManager.Now,
            }; 
        }
    }
}
