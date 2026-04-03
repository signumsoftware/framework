using Signum.API.Json;
using Signum.API;
using Signum.Mailing;
using Signum.Mailing.Pop3;
using Signum.Scheduler;
using Signum.Utilities.Reflection;
using System;
using System.Linq;
using System.Text.Json;
using Signum.Mailing.Reception;

namespace Signum.Mailing.Pop3;

public static class Pop3ConfigurationLogic
{
    public static CancellationToken CancelationToken;

    public static Func<EmailReceptionConfigurationEntity, IPop3Client> GetPop3Client = null!;
    public static Func<string, string> EncryptPassword = s => s;
    public static Func<string, string> DecryptPassword = s => s;
    public static int MaxReceptionPerTime = 15;

    public static void Start(SchemaBuilder sb, Func<EmailReceptionConfigurationEntity, IPop3Client> getPop3Client, Func<string, string>? encryptPassword = null, Func<string, string>? decryptPassword = null)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        GetPop3Client = getPop3Client;

        if (encryptPassword != null)
            EncryptPassword = encryptPassword;

        if (decryptPassword != null)
            DecryptPassword = decryptPassword;

        sb.Settings.AssertImplementedBy((EmailReceptionConfigurationEntity e) => e.Service, typeof(Pop3EmailReceptionServiceEntity));



        EmailReceptionLogic.EmailReceptionServices.Register(new Func<Pop3EmailReceptionServiceEntity, EmailReceptionConfigurationEntity, ScheduledTaskContext, EmailReceptionEntity>(ReceiveEmails));

        if (sb.WebServerBuilder != null)
        {
            var piPassword = ReflectionTools.GetPropertyInfo((Pop3EmailReceptionServiceEntity e) => e.Password);
            var pcs = SignumServer.WebEntityJsonConverterFactory.GetPropertyConverters(typeof(Pop3EmailReceptionServiceEntity));
            pcs.GetOrThrow("password").CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) => { };
            pcs.Remove("newPassword"); /* Pop3EmailReceptionServiceEntity already has NewPassword property */
            pcs.Add("newPassword", new PropertyConverter
            {
                AvoidValidate = true,
                CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) => { },
                CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
                {
                    var sm = EntityJsonContext.CurrentSerializationPath!.CurrentSerializationMetadata();

                    ctx.Factory.AssertCanWrite(ctx.ParentPropertyRoute.Add(piPassword), ctx.Entity, sm);

                    var password = reader.GetString()!;

                    ((Pop3EmailReceptionServiceEntity)ctx.Entity).Password = Pop3ConfigurationLogic.EncryptPassword(password);
                }
            });
        }
    }

    public static event Func<EmailReceptionConfigurationEntity, IDisposable>? SurroundReceiveEmail;






    public static EmailReceptionEntity ReceiveEmails(Pop3EmailReceptionServiceEntity service, EmailReceptionConfigurationEntity config, ScheduledTaskContext ctx)
    {
        if (!EmailLogic.Configuration.ReciveEmails)
            throw new InvalidOperationException("EmailLogic.Configuration.ReciveEmails is set to false");

        using (HeavyProfiler.Log("ReciveEmails"))
        using (Disposable.Combine(SurroundReceiveEmail, func => func(config)))
        {
            EmailReceptionEntity reception = Transaction.ForceNew().Using(tr => tr.Commit(
                new EmailReceptionEntity { EmailReceptionConfiguration = config.ToLite(), StartDate = Clock.Now }.Save()));

            var now = Clock.Now;
            try
            {
                using (var client = GetPop3Client(config))
                {

                    int messageInfosNum = 0;

                    List<MessageUid> messagesToSave = GetMessagesToSave(config, client, out messageInfosNum);

                    using (var tr = Transaction.ForceNew())
                    {
                        reception.ServerEmails = messageInfosNum;
                        reception.NewEmails = messagesToSave.Count;
                        reception.Save();
                        tr.Commit();
                    }

                    bool anomalousReception = false;
                    string lastSuid = "";
                    foreach (var mi in messagesToSave)
                    {


                        if (CancelationToken.IsCancellationRequested || ctx.CancellationToken.IsCancellationRequested)
                            break;

                        var sent = SaveEmail(config, reception, client, mi, ref anomalousReception);
                        lastSuid = mi.Uid;
                        DeleteServerMessageIfNecessary(config, now, client, mi, sent);
                    }

                    using (var tr = Transaction.ForceNew())
                    {
                        reception.EndDate = Clock.Now;
                        reception.LastServerMessageUID = lastSuid;
                        reception.MailsFromDifferentAccounts = anomalousReception;
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
                    using (var tr = Transaction.ForceNew())
                    {
                        reception.EndDate = Clock.Now;
                        reception.Exception = ex.ToLite();
                        reception.Save();
                        tr.Commit();
                    }
                }
                catch { }
            }

            EmailReceptionLogic.ReceptionComunication?.Invoke(reception);

            return reception;
        }
    }

    private static List<MessageUid> GetMessagesToSave(EmailReceptionConfigurationEntity config, IPop3Client client, out int messageCount)
    {
        var messageInfos = client.GetMessageInfos().OrderBy(m => m.Number);
        messageCount = messageInfos.Count();


        if (config.CompareInbox == CompareInbox.Full)
        {
            var already = messageInfos.Select(a => a.Uid).Chunk(50).SelectMany(l =>
                        (from em in Database.Query<EmailMessageEntity>()
                         let ri = em.Mixin<EmailReceptionMixin>().ReceptionInfo
                         where ri != null && l.Contains(ri.UniqueId)
                         select KeyValuePair.Create(ri.UniqueId, (DateTime?)ri.SentDate)))
                        .ToDictionary();

            return messageInfos.Where(m => !already.ContainsKey(m.Uid)).ToList();
        }


        var lastsEmails = Database.Query<EmailMessageEntity>()
            .Where(e => e.Mixin<EmailReceptionMixin>().ReceptionInfo!.Reception.Entity.EmailReceptionConfiguration.Is(config))
            .Select(d => new { d.CreationDate, d.Mixin<EmailReceptionMixin>().ReceptionInfo!.UniqueId })
            .OrderByDescending(c => c.CreationDate).Take(MaxReceptionPerTime).ToDictionary(e => e.UniqueId);


        if (lastsEmails != null && lastsEmails.Any())
        {
            var messageJoinDict = messageInfos.ToDictionary(e => e.Uid).OuterJoinDictionarySC(lastsEmails, (key, v1, v2) => new { key, v1, v2 });
            var messageMachings = messageJoinDict.Where(e => e.Value.v1 != null && e.Value.v2 != null).ToList();

            if (!messageMachings.Any())
                return messageInfos.ToList();

            var maxId = !messageMachings.Any() ? 0 : messageMachings.Select(e => e.Value.v1!.Value.Number).Max();

            return messageInfos.Where(m => m.Number > maxId).OrderBy(m => m.Number)
                .Take(MaxReceptionPerTime).ToList();// max maxReceptionForTime message per time
        }
        else
        {
            // the first time only get the last maxReceptionForTime messages
            return messageInfos.OrderByDescending(m => m.Number).Take(MaxReceptionPerTime).ToList();
        }
    }

    private static DateTime? SaveEmail(EmailReceptionConfigurationEntity config, EmailReceptionEntity reception, IPop3Client client, MessageUid mi, ref bool anomalousReception)
    {
        DateTime? sent = null;

        {
            using (OperationLogic.AllowSave<EmailMessageEntity>())
            using (var tr = Transaction.ForceNew())
            {
                string rawContent = "";
                try
                {
                    var email = client.GetMessage(mi, reception.ToLite());
                    email.Subject = email.Subject == null ? "No Subject" : email.Subject.Replace('\n', ' ').Replace('\r', ' ');

                    if (email.Recipients.IsEmpty())
                    {
                        email.Recipients.Add(new EmailRecipientEmbedded
                        {
                            EmailAddress = config.EmailAddress ?? "",
                            Kind = EmailRecipientKind.To,
                        });
                    }

                    email.SetCalculateHash();

                    var duplicateList = Database.Query<EmailMessageEntity>()
                           .Where(a => a.BodyHash == email.BodyHash)
                           .Select(a => new { l = a.ToLite(), date = (DateTime?)a.Mixin<EmailReceptionMixin>().ReceptionInfo!.ReceivedDate, bh = a.BodyHash, suid = a.Mixin<EmailReceptionMixin>().ReceptionInfo!.UniqueId })
                           .Distinct().ToList();

                    if (duplicateList.Any(e => e.suid == email.Mixin<EmailReceptionMixin>().ReceptionInfo!.UniqueId))
                    {
                        // for some reason the account is receiving emails where the account is not in the recipients and has already been previously received
                        anomalousReception = true;
                    }
                    else
                    {
                        var duplicate = duplicateList.OrderByDescending(e => e.date).FirstOrDefault();

                        EmailMessageEntity? dup = null;
                        if (duplicate != null)
                            dup = duplicate.l.Retrieve();

                        if (duplicate != null && AreDuplicates(email, dup!))
                        {
                            email.AssignEntities(dup!);
                            AssociateDuplicateEmail?.Invoke(email, dup!);
                        }
                        else
                        {
                            AssociateNewEmail?.Invoke(email);
                        }

                        email.Save();

                        sent = email.Mixin<EmailReceptionMixin>().ReceptionInfo!.SentDate;
                    }
                    tr.Commit();
                }
                catch (Exception e)
                {
                    e.Data["rawContent"] = rawContent;

                    var ex = e.LogException();

                    using (var tr2 = Transaction.ForceNew())
                    {
                        new EmailReceptionExceptionEntity
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



    private static void DeleteServerMessageIfNecessary(EmailReceptionConfigurationEntity config, DateTime now, IPop3Client client, MessageUid mi, DateTime? sent)
    {
        if ((config.DeleteMessagesAfter != null && sent != null &&
             sent.Value.Date.AddDays(config.DeleteMessagesAfter.Value) < Clock.Now.Date))
        {
            DeleteServerMessage(now, client, mi);
        }
    }

    private static void DeleteServerMessage(DateTime now, IPop3Client client, MessageUid mi)
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

    private static DateTime? SaveEmail(EmailReceptionConfigurationEntity config, EmailReceptionEntity reception, IPop3Client client, MessageUid mi)
    {
        DateTime? sent = null;

        {
            using (OperationLogic.AllowSave<EmailMessageEntity>())
            using (var tr = Transaction.ForceNew())
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
                            EmailAddress = config.EmailAddress!,
                            Kind = EmailRecipientKind.To,
                        });
                    }
                    email.SetCalculateHash();

                    Lite<EmailMessageEntity>? duplicate = Database.Query<EmailMessageEntity>()
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

                    using (var tr2 = Transaction.ForceNew())
                    {
                        new EmailReceptionExceptionEntity
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

public interface IPop3Client : IDisposable
{
    List<MessageUid> GetMessageInfos();

    EmailMessageEntity GetMessage(MessageUid messageInfo, Lite<EmailReceptionEntity> reception);

    void DeleteMessage(MessageUid messageInfo);
    void Disconnect();
}

public struct MessageUid
{
    public MessageUid(string uid, int number, int size)
    {
        Uid = uid;
        Number = number;
        Size = size;
    }

    public readonly string Uid;
    public readonly int Number;
    public readonly int Size;
}
