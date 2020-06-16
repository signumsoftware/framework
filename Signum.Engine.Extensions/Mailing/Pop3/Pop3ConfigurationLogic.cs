using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Engine.Scheduler;
using Signum.Entities;
using Signum.Entities.Mailing;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Engine.Extensions.Mailing.Pop3;

namespace Signum.Engine.Mailing.Pop3
{
    public static class Pop3ConfigurationLogic
    {
        [AutoExpressionField]
        public static IQueryable<Pop3ReceptionEntity> Receptions(this Pop3ConfigurationEntity c) => 
            As.Expression(() => Database.Query<Pop3ReceptionEntity>().Where(r => r.Pop3Configuration.Is(c)));

        [AutoExpressionField]
        public static IQueryable<EmailMessageEntity> EmailMessages(this Pop3ReceptionEntity r) => 
            As.Expression(() => Database.Query<EmailMessageEntity>().Where(m => m.Mixin<EmailReceptionMixin>().ReceptionInfo!.Reception.Is(r)));

        [AutoExpressionField]
        public static IQueryable<ExceptionEntity> Exceptions(this Pop3ReceptionEntity e) => 
            As.Expression(() => Database.Query<Pop3ReceptionExceptionEntity>().Where(a => a.Reception.Is(e)).Select(a => a.Exception.Entity));

        [AutoExpressionField]
        public static Pop3ReceptionEntity? Pop3Reception(this ExceptionEntity ex) => 
            As.Expression(() => Database.Query<Pop3ReceptionExceptionEntity>().Where(re => re.Exception.Is(ex)).Select(re => re.Reception.Entity).SingleOrDefaultEx());

        public static Func<Pop3ConfigurationEntity, IPop3Client> GetPop3Client = null!;


        public static Action<Pop3ReceptionEntity>? ReceptionComunication;

        public static void Start(SchemaBuilder sb, Func<Pop3ConfigurationEntity, IPop3Client> getPop3Client)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                GetPop3Client = getPop3Client;

                MixinDeclarations.AssertDeclared(typeof(EmailMessageEntity), typeof(EmailReceptionMixin));

                sb.Include<Pop3ConfigurationEntity>()
                    .WithSave(Pop3ConfigurationOperation.Save)
                    .WithQuery(() => s => new
                    {
                        Entity = s,
                        s.Id,
                        s.Host,
                        s.Port,
                        s.Username,
                        s.EnableSSL
                    });
                sb.Include<Pop3ReceptionEntity>();
                sb.Include<Pop3ReceptionExceptionEntity>();

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
                        e.Package,
                        e.Exception,
                    });

                QueryLogic.Queries.Register(typeof(Pop3ReceptionEntity), () => DynamicQueryCore.Auto(
                from s in Database.Query<Pop3ReceptionEntity>()
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
                .ColumnDisplayName(a => a.EmailMessages, () => typeof(EmailMessageEntity).NicePluralName())
                .ColumnDisplayName(a => a.Exceptions, () => typeof(ExceptionEntity).NicePluralName()));

                QueryLogic.Expressions.Register((Pop3ConfigurationEntity c) => c.Receptions(), () => typeof(Pop3ReceptionEntity).NicePluralName());
                QueryLogic.Expressions.Register((Pop3ReceptionEntity r) => r.EmailMessages(), () => typeof(EmailMessageEntity).NicePluralName());
                QueryLogic.Expressions.Register((Pop3ReceptionEntity r) => r.Exceptions(), () => typeof(ExceptionEntity).NicePluralName());
                QueryLogic.Expressions.Register((ExceptionEntity r) => r.Pop3Reception(), () => typeof(Pop3ReceptionEntity).NiceName());

                new Graph<Pop3ReceptionEntity>.ConstructFrom<Pop3ConfigurationEntity>(Pop3ConfigurationOperation.ReceiveEmails)
                {
                    CanBeNew = true,
                    CanBeModified = true,
                    Construct = (e, _) =>
                    {
                        using (Transaction tr = Transaction.None())
                        {
                            var result = e.ReceiveEmails();
                            return tr.Commit(result);
                        }
                    }
                }.Register();

                SchedulerLogic.ExecuteTask.Register((Pop3ConfigurationEntity smtp, ScheduledTaskContext ctx) => smtp.ReceiveEmails().ToLite());

                SimpleTaskLogic.Register(Pop3ConfigurationAction.ReceiveAllActivePop3Configurations, (ScheduledTaskContext ctx) =>
                {
                    if (!EmailLogic.Configuration.ReciveEmails)
                        throw new InvalidOperationException("EmailLogic.Configuration.ReciveEmails is set to false");

                    foreach (var item in Database.Query<Pop3ConfigurationEntity>().Where(a => a.Active).ToList())
                    {
                        item.ReceiveEmails();
                    }

                    return null;
                });
            }
        }

        public static event Func<Pop3ConfigurationEntity, IDisposable>? SurroundReceiveEmail;

        public static Pop3ReceptionEntity ReceiveEmails(this Pop3ConfigurationEntity config)
        {
            if (config.FullComparation)
                return ReceiveEmailsFullComparation(config);
            else
                return ReceiveEmailsPartialComparation(config);
        }

        public static Pop3ReceptionEntity ReceiveEmailsPartialComparation(this Pop3ConfigurationEntity config)
        {
            if (!EmailLogic.Configuration.ReciveEmails)
                throw new InvalidOperationException("EmailLogic.Configuration.ReciveEmails is set to false");

            using (HeavyProfiler.Log("ReciveEmails"))
            using (Disposable.Combine(SurroundReceiveEmail, func => func(config)))
            {
                Pop3ReceptionEntity reception = Transaction.ForceNew().Using(tr => tr.Commit(
                    new Pop3ReceptionEntity { Pop3Configuration = config.ToLite(), StartDate = TimeZoneManager.Now }.Save()));

                var now = TimeZoneManager.Now;
                try
                {
                    using (var client = GetPop3Client(config))
                    {
                        var messageInfos = client.GetMessageInfos().OrderBy(m => m.Number);


                        var lastsEmails = Database.Query<EmailMessageEntity>()
                            .Where(e => e.Mixin<EmailReceptionMixin>().ReceptionInfo!.Reception.Entity.Pop3Configuration.Is(config))
                            .Select(d => new { d.CreationDate, d.Mixin<EmailReceptionMixin>().ReceptionInfo!.UniqueId })
                            .OrderByDescending(c => c.CreationDate).Take(10).ToDictionary(e => e.UniqueId!); /*CSBUG*/


                        List<MessageUid> messagesToSave;
                        if (lastsEmails.Any())
                        {
                            var messageJoinDict = messageInfos.ToDictionary(e => e.Uid).OuterJoinDictionarySC(lastsEmails, (key, v1, v2) => new { key, v1, v2 });
                            var messageMachings = messageJoinDict.Where(e => e.Value.v1 != null && e.Value.v2 != null).ToList();
                            messagesToSave = !messageMachings.Any()? messageInfos.ToList(): messageInfos.Where(m => m.Number > messageMachings.Select(e => e.Value.v1.Value.Number).Max()).ToList();
                        }
                        else
                            messagesToSave = messageInfos.ToList();

                        using (Transaction tr = Transaction.ForceNew())
                        {
                            reception.NewEmails = messagesToSave.Count;
                            reception.Save();
                            tr.Commit();
                        }

                        foreach (var mi in messagesToSave)
                        {

                            var sent = SaveEmail(config, reception, client, mi);

                            DeleteSavedEmail(config, now, client, mi, sent);
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

                ReceptionComunication?.Invoke(reception);

                return reception;
            }
        }

        private static void DeleteSavedEmail(Pop3ConfigurationEntity config, DateTime now, IPop3Client client, MessageUid mi, DateTime? sent)
        {
            if (config.DeleteMessagesAfter != null && sent != null &&
                 sent.Value.Date.AddDays(config.DeleteMessagesAfter.Value) < TimeZoneManager.Now.Date)
            {
                client.DeleteMessage(mi);

                (from em in Database.Query<EmailMessageEntity>()
                 let ri = em.Mixin<EmailReceptionMixin>().ReceptionInfo
                 where ri != null && ri.UniqueId == mi.Uid
                 select em)
                 .UnsafeUpdate()
                 .Set(em => em.Mixin<EmailReceptionMixin>().ReceptionInfo!.DeletionDate, em => now)
                 .Execute();
            }
        }

        public static Pop3ReceptionEntity ReceiveEmailsFullComparation(this Pop3ConfigurationEntity config)
        {
            if (!EmailLogic.Configuration.ReciveEmails)
                throw new InvalidOperationException("EmailLogic.Configuration.ReciveEmails is set to false");

            using (HeavyProfiler.Log("ReciveEmails"))
            using (Disposable.Combine(SurroundReceiveEmail, func => func(config)))
            {
                Pop3ReceptionEntity reception = Transaction.ForceNew().Using(tr => tr.Commit(
                    new Pop3ReceptionEntity { Pop3Configuration = config.ToLite(), StartDate = TimeZoneManager.Now }.Save()));

                var now = TimeZoneManager.Now;
                try
                {
                    using (var client = GetPop3Client(config))
                    {
                        var messageInfos = client.GetMessageInfos();

                        var already = messageInfos.Select(a => a.Uid).GroupsOf(50).SelectMany(l =>
                            (from em in Database.Query<EmailMessageEntity>()
                             let ri = em.Mixin<EmailReceptionMixin>().ReceptionInfo
                             where ri != null && l.Contains(ri.UniqueId)
                             select KeyValuePair.Create(ri.UniqueId, (DateTime?)ri.SentDate))).ToDictionary();

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
                                sent = SaveEmail(config, reception, client, mi);

                            DeleteSavedEmail(config, now, client, mi, sent);
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

                ReceptionComunication?.Invoke(reception);

                return reception;
            }
        }

        private static DateTime? SaveEmail(Pop3ConfigurationEntity config, Pop3ReceptionEntity reception, IPop3Client client, MessageUid mi)
        {
            DateTime? sent = null;

            {
                using (OperationLogic.AllowSave<EmailMessageEntity>())
                using (Transaction tr = Transaction.ForceNew())
                {
                    string? rawContent = null;
                    try
                    {
                        var email = client.GetMessage(mi, reception.ToLite());

                        email.Subject = email.Subject == null ? "No Subject" : email.Subject.Replace('\n', ' ').Replace('\r', ' ');

                        if (email.Recipients.IsEmpty())
                        {
                            email.Recipients.Add(new EmailRecipientEmbedded
                            {
                                EmailAddress = config.Username!,
                                Kind = EmailRecipientKind.To,
                            });
                        }

                        Lite<EmailMessageEntity> duplicate = Database.Query<EmailMessageEntity>()
                            .Where(a => a.BodyHash == email.BodyHash)
                            .Select(a => a.ToLite())
                            .FirstOrDefault();

                        if (duplicate != null && AreDuplicates(email, duplicate.RetrieveAndRemember()))
                        {
                            var dup = duplicate.Entity;

                            email.AssignEntities(dup);

                            AssociateDuplicateEmail?.Invoke(email, dup);
                        }
                        else
                        {
                            AssociateNewEmail?.Invoke(email);
                        }

                        email.Save();

                        sent = email.Mixin<EmailReceptionMixin>().ReceptionInfo!.SentDate;

                        tr.Commit();
                    }
                    catch (Exception e)
                    {
                        e.Data["rawContent"] = rawContent;

                        var ex = e.LogException();

                        using (Transaction tr2 = Transaction.ForceNew())
                        {
                            new Pop3ReceptionExceptionEntity
                            {
                                Exception = ex.ToLite(),
                                Reception = reception.ToLite()
                            }.Save();

                            tr2.Commit();
                        }
                    }
                }
            }

            return sent;
        }

        private static void AssignEntities(this EmailMessageEntity email, EmailMessageEntity dup)
        {
            email.Target = dup.Target;
            foreach (var att in email.Attachments)
                att.File = dup.Attachments.FirstEx(a => a.Similar(att)).File;

            email.From.EmailOwner = dup.From.EmailOwner;
            foreach (var rec in email.Recipients.Where(a => a.Kind != EmailRecipientKind.Bcc))
                rec.EmailOwner = dup.Recipients.FirstEx(a => a.GetHashCode() == rec.GetHashCode()).EmailOwner;
        }

        private static bool AreDuplicates(EmailMessageEntity email, EmailMessageEntity dup)
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

        public static Action<EmailMessageEntity>? AssociateNewEmail;
        public static Action<EmailMessageEntity, EmailMessageEntity>? AssociateDuplicateEmail;
    }
}
