using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Files;
using Signum.Engine.Mailing.Pop3;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Engine.Scheduler;
using Signum.Entities;
using Signum.Entities.Files;
using Signum.Entities.Mailing;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Entities.Basics;
using System.Net.Mime;
using System.Threading;
using Signum.Engine.Extensions.Mailing.Pop3;

namespace Signum.Engine.Mailing.Pop3
{
    public static class Pop3ConfigurationLogic
    {
        static Expression<Func<Pop3ConfigurationDN, IQueryable<Pop3ReceptionDN>>> ReceptionsExpression =
            c => Database.Query<Pop3ReceptionDN>().Where(r => r.Pop3Configuration.RefersTo(c));
        public static IQueryable<Pop3ReceptionDN> Receptions(this Pop3ConfigurationDN c)
        {
            return ReceptionsExpression.Evaluate(c);
        }

        static Expression<Func<Pop3ReceptionDN, IQueryable<EmailMessageDN>>> EmailMessagesExpression =
            r => Database.Query<EmailMessageDN>().Where(m => m.Mixin<EmailReceptionMixin>().ReceptionInfo.Reception.RefersTo(r));
        public static IQueryable<EmailMessageDN> EmailMessages(this Pop3ReceptionDN r)
        {
            return EmailMessagesExpression.Evaluate(r);
        }

        static Expression<Func<Pop3ReceptionDN, IQueryable<ExceptionDN>>> ExceptionsExpression =
            e => Database.Query<Pop3ReceptionExceptionDN>().Where(a => a.Reception.RefersTo(e)).Select(a => a.Exception.Entity);
        public static IQueryable<ExceptionDN> Exceptions(this Pop3ReceptionDN e)
        {
            return ExceptionsExpression.Evaluate(e);
        }


        static Expression<Func<ExceptionDN, Pop3ReceptionDN>> Pop3ReceptionExpression =
            ex => Database.Query<Pop3ReceptionExceptionDN>().Where(re => re.Exception.RefersTo(ex)).Select(re => re.Reception.Entity).SingleOrDefaultEx();
        public static Pop3ReceptionDN Pop3Reception(this ExceptionDN entity)
        {
            return Pop3ReceptionExpression.Evaluate(entity);
        }

        public static Func<Pop3ConfigurationDN, IPop3Client> GetPop3Client;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, Func<Pop3ConfigurationDN, IPop3Client> getPop3Client)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                GetPop3Client = getPop3Client;

                FilePathLogic.Register(EmailFileType.Attachment, new FileTypeAlgorithm { CalculateSufix = FileTypeAlgorithm.YearMonth_Guid_Filename_Sufix });

                MixinDeclarations.AssertDeclared(typeof(EmailMessageDN), typeof(EmailReceptionMixin));

                MimeType.CacheExtension.TryAdd("message/rfc822", ".eml");

                sb.Include<Pop3ConfigurationDN>();
                sb.Include<Pop3ReceptionDN>();
                sb.Include<Pop3ReceptionExceptionDN>();

                dqm.RegisterQuery(typeof(EmailMessageDN), () =>
                   from e in Database.Query<EmailMessageDN>()
                   select new
                   {
                       Entity = e,
                       e.Id,
                       e.From,
                       e.Subject,
                       e.Template,
                       e.State,
                       e.Sent,
                       SentDate = (DateTime?)e.Mixin<EmailReceptionMixin>().ReceptionInfo.SentDate,
                       e.Package,
                       e.Exception,
                   });

                dqm.RegisterQuery(typeof(Pop3ConfigurationDN), () =>
                    from s in Database.Query<Pop3ConfigurationDN>()
                    select new
                    {
                        Entity = s,
                        s.Id,
                        s.Host,
                        s.Port,
                        s.Username,
                        s.EnableSSL
                    });

                dqm.RegisterQuery(typeof(Pop3ReceptionDN), () => DynamicQuery.DynamicQuery.Auto(
                 from s in Database.Query<Pop3ReceptionDN>()
                 select new
                 {
                     Entity = s,
                     s.Id,
                     s.Pop3Configuration,
                     s.StartDate,
                     s.EndDate,
                     s.NewEmails,
                     EmailMessages = s.EmailMessages().Count(),
                     Exceptions = s.Exceptions().Count(),
                     s.Exception,
                 })
                 .ColumnDisplayName(a => a.EmailMessages, () => typeof(EmailMessageDN).NicePluralName())
                 .ColumnDisplayName(a => a.Exceptions, () => typeof(ExceptionDN).NicePluralName()));

                dqm.RegisterQuery(typeof(Pop3ConfigurationDN), () =>
                    from s in Database.Query<Pop3ConfigurationDN>()
                    select new
                    {
                        Entity = s,
                        s.Id,
                        s.Host,
                        s.Port,
                        s.Username,
                        s.EnableSSL
                    });

                dqm.RegisterExpression((Pop3ConfigurationDN c) => c.Receptions(), () => typeof(Pop3ReceptionDN).NicePluralName());
                dqm.RegisterExpression((Pop3ReceptionDN r) => r.EmailMessages(), () => typeof(EmailMessageDN).NicePluralName());
                dqm.RegisterExpression((Pop3ReceptionDN r) => r.Exceptions(), () => typeof(ExceptionDN).NicePluralName());
                dqm.RegisterExpression((ExceptionDN r) => r.Pop3Reception(), () => typeof(Pop3ReceptionDN).NiceName());

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
                    Execute = (e, _) =>
                    {
                        using (Transaction tr = Transaction.None())
                        {
                            e.ReceiveEmails();
                            tr.Commit();
                        }
                    }
                }.Register();

                SchedulerLogic.ExecuteTask.Register((Pop3ConfigurationDN smtp) => smtp.ReceiveEmails().ToLite());

                SimpleTaskLogic.Register(Pop3ConfigurationAction.ReceiveAllActivePop3Configurations, () =>
                {
                    if (!EmailLogic.Configuration.ReciveEmails)
                        throw new InvalidOperationException("EmailLogic.Configuration.ReciveEmails is set to false");

                    foreach (var item in Database.Query<Pop3ConfigurationDN>().Where(a => a.Active).ToList())
                    {
                        item.ReceiveEmails();
                    }

                    return null;
                });
            }
        }

        public static event Func<Pop3ConfigurationDN, IDisposable> SurroundReceiveEmail;

        public static Pop3ReceptionDN ReceiveEmails(this Pop3ConfigurationDN config)
        {
            if (!EmailLogic.Configuration.ReciveEmails)
                throw new InvalidOperationException("EmailLogic.Configuration.ReciveEmails is set to false");

            using (Disposable.Combine(SurroundReceiveEmail, func => func(config)))
            {
                Pop3ReceptionDN reception = Transaction.ForceNew().Using(tr => tr.Commit(
                    new Pop3ReceptionDN { Pop3Configuration = config.ToLite(), StartDate = TimeZoneManager.Now }.Save()));

                var now = TimeZoneManager.Now;
                try
                {
                    using (var client = GetPop3Client(config))
                    {
                        var messageInfos = client.GetMessageInfos();

                        var already = messageInfos.Select(a => a.Uid).GroupsOf(50).SelectMany(l =>
                            (from em in Database.Query<EmailMessageDN>()
                             let ri = em.Mixin<EmailReceptionMixin>().ReceptionInfo
                             where ri != null && l.Contains(ri.UniqueId)
                             select KVP.Create(ri.UniqueId, (DateTime?)ri.SentDate))).ToDictionary();

                        using (Transaction tr = Transaction.ForceNew())
                        {
                            reception.NewEmails = messageInfos.Count - already.Count;
                            reception.Save();
                            tr.Commit();
                        }

                        foreach (var mi in messageInfos)
                        {
                            var sent = already.TryGetS(mi.Uid);

                            if (sent == null)
                            {
                                using (OperationLogic.AllowSave<EmailMessageDN>())
                                using (Transaction tr = Transaction.ForceNew())
                                {
                                    string rawContent = null;
                                    try
                                    {
                                        var email = client.GetMessage(mi, reception.ToLite());

                                        if (email.Recipients.IsEmpty())
                                        {
                                            email.Recipients.Add(new EmailRecipientDN
                                            {
                                                EmailAddress = config.Username,
                                                Kind = EmailRecipientKind.To,
                                            });
                                        }

                                        Lite<EmailMessageDN> duplicate = Database.Query<EmailMessageDN>()
                                            .Where(a => a.BodyHash == email.BodyHash)
                                            .Select(a => a.ToLite())
                                            .FirstOrDefault();

                                        if (duplicate != null && AreDuplicates(email, duplicate.Retrieve()))
                                        {
                                            var dup = duplicate.Entity;

                                            email.AssignEntities(dup);

                                            if (AssociateDuplicateEmail != null)
                                                AssociateDuplicateEmail(email, dup);
                                        }
                                        else
                                        {
                                            if (AssociateNewEmail != null)
                                                AssociateNewEmail(email);
                                        }

                                        email.Save();

                                        sent = email.Mixin<EmailReceptionMixin>().ReceptionInfo.SentDate;

                                        tr.Commit();
                                    }
                                    catch (Exception e)
                                    {
                                        e.Data["rawContent"] = rawContent;

                                        var ex = e.LogException();

                                        using (Transaction tr2 = Transaction.ForceNew())
                                        {
                                            new Pop3ReceptionExceptionDN
                                            {
                                                Exception = ex.ToLite(),
                                                Reception = reception.ToLite()
                                            }.Save();

                                            tr2.Commit();
                                        }
                                    }
                                }
                            }

                            if (config.DeleteMessagesAfter != null && sent != null &&
                                 sent.Value.Date.AddDays(config.DeleteMessagesAfter.Value) < TimeZoneManager.Now.Date)
                            {
                                client.DeleteMessage(mi);

                                (from em in Database.Query<EmailMessageDN>()
                                 let ri = em.Mixin<EmailReceptionMixin>().ReceptionInfo
                                 where ri != null && ri.UniqueId == mi.Uid
                                 select em)
                                 .UnsafeUpdate()
                                 .Set(em => em.Mixin<EmailReceptionMixin>().ReceptionInfo.DeletionDate, em => now)
                                 .Execute();
                            }
                        }

                        using (Transaction tr = Transaction.ForceNew())
                        {
                            reception.EndDate = TimeZoneManager.Now;
                            reception.Save();
                            tr.Commit();
                        }

                        client.Disconnect(); //Delete messages now
                    }
                }
                catch (Exception e)
                {
                    var ex = e.LogException();

                    try
                    {
                        using (Transaction tr = Transaction.ForceNew())
                        {
                            reception.EndDate = TimeZoneManager.Now;
                            reception.Exception = ex.ToLite();
                            reception.Save();
                            tr.Commit();
                        }
                    }
                    catch { }
                }

                return reception;
            }
        }

        private static void AssignEntities(this EmailMessageDN email, EmailMessageDN dup)
        {
            email.Target = dup.Target;
            foreach (var att in email.Attachments)
                att.File = dup.Attachments.FirstEx(a => a.Similar(att)).File;

            email.From.EmailOwner = dup.From.EmailOwner;
            foreach (var rec in email.Recipients.Where(a => a.Kind != EmailRecipientKind.Bcc))
                rec.EmailOwner = dup.Recipients.FirstEx(a => a.GetHashCode() == rec.GetHashCode()).EmailOwner;
        }

        private static bool AreDuplicates(EmailMessageDN email, EmailMessageDN dup)
        {
            if (!dup.Recipients.Where(a => a.Kind != EmailRecipientKind.Bcc).OrderBy(a => a.GetHashCode())
                .SequenceEqual(email.Recipients.Where(a => a.Kind != EmailRecipientKind.Bcc).OrderBy(a => a.GetHashCode())))
                return false;

            if (!dup.From.Equals(email.From))
                return false;

            if (dup.Attachments.Count != email.Attachments.Count || !dup.Attachments.All(a => email.Attachments.Any(a2 => a2.Similar(a))))
                return false;

            return true;
        }

        public static Action<EmailMessageDN> AssociateNewEmail;
        public static Action<EmailMessageDN, EmailMessageDN> AssociateDuplicateEmail;
    }
}
