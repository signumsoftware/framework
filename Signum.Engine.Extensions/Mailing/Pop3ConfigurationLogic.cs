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

namespace Signum.Engine.Mailing
{
    public static class Pop3ConfigurationLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                FilePathLogic.Register(EmailFileType.Attachment, new FileTypeAlgorithm { CalculateSufix = FileTypeAlgorithm.YearMonth_Guid_Filename_Sufix });

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
                    Execute = (e, _) =>
                    {
                        using(Transaction tr = Transaction.None())
                        {
                            e.ReceiveEmails();
                            tr.Commit();
                        }
                    }
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

            try
            {
                using (Pop3Client client = new Pop3Client(config.Username, config.Password, config.Host, config.Port, config.EnableSSL))
                {
                    client.Connect();

                    var dic = client.GetMessages();

                    reception.NumberOfMails = dic.Count;

                    if (reception.NumberOfMails == 0)
                        return null;

                    using (Transaction tr = Transaction.ForceNew())
                    {
                        reception.Save();
                        tr.Commit();
                    }

                    int maxids = Math.Min(dic.Count, config.MaxDownloadEmails);

                    for (int i = 1; i <= maxids; i++)
                    {
                        using (Transaction tr = Transaction.ForceNew())
                        {
                            try
                            {
                                string rawContent = client.GetMessage(i);

                                MailMessage mm = new StringReader(rawContent).Using(MailMimeParser.Parse);

                                EmailMessageDN email = ToEmailMessage(mm, rawContent);

                                Lite<EmailMessageDN> duplicate = Database.Query<EmailMessageDN>()
                                    .Where(a => a.BodyHash == email.BodyHash)
                                    .Select(a => a.ToLite())
                                    .SingleOrDefaultEx();

                                if (duplicate != null && AreDuplicates(email, duplicate.Retrieve()))
                                {
                                    var dup = duplicate.Retrieve();
                                    dup.Duplicates++;
                                    dup.Save();
                                }
                                else
                                {
                                    email.Reception = reception.ToLite();

                                    if (AssociateWithEntities != null)
                                        AssociateWithEntities(email);

                                    using (OperationLogic.AllowSave<EmailMessageDN>())
                                        email.Save();
                                }

                                if (config.DeleteAfterReception)
                                    client.DeleteMessage(i);
                            }
                            catch (Exception e)
                            {
                                e.LogException();
                            }

                            tr.Commit();
                        }
                    }

                    using (Transaction tr = Transaction.ForceNew())
                    {
                        reception.Save();
                        tr.Commit();
                    }

                    reception.EndDate = TimeZoneManager.Now;
                    reception.Save();

                    client.Disconnect(); //Delete messages now
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

        static LambdaComparer<EmailAttachmentDN, string> fileComparer = new LambdaComparer<EmailAttachmentDN, string>(fp => fp.File.FileName);

        private static bool AreDuplicates(EmailMessageDN email, EmailMessageDN dup)
        {
            if (!dup.Recipients.SequenceEqual(email.Recipients))
                return false;

            if (!dup.From.Equals(email.From))
                return false;

            if (!dup.Attachments.SequenceEqual(email.Attachments, fileComparer))
                return false;

            return true;
        }

        public static Action<EmailMessageDN> AssociateWithEntities;

        private static EmailMessageDN ToEmailMessage(MailMessage mm, string rawContent)
        {
            var dn = new EmailMessageDN
            {
                EditableMessage = false,
                From = new EmailAddressDN(mm.From),
                Recipients =
                   mm.To.Select(ma => new EmailRecipientDN(ma, EmailRecipientKind.To)).Concat(
                   mm.CC.Select(ma => new EmailRecipientDN(ma, EmailRecipientKind.CC))).Concat(
                   mm.Bcc.Select(ma => new EmailRecipientDN(ma, EmailRecipientKind.Bcc))).ToMList(),
                State = EmailMessageState.Received,
                Subject = mm.Subject,
                Received = TimeZoneManager.Now,
                RawContent = rawContent,
                Attachments = mm.Attachments.Select(a => 
                    new EmailAttachmentDN 
                    {
                        ContentId = a.ContentId,
                        File = new FilePathDN(EmailFileType.Attachment, a.ContentType.Name, a.ContentStream.ReadAllBytes()).Save(), 
                        Type = EmailAttachmentType.Attachment 
                    }).ToMList()
            };


            DateTime parse;
            if (DateTime.TryParse(mm.Headers["Date"], out parse))
            {
                dn.Sent = parse;
            }

            if (mm.Body.HasText())
            {
                dn.IsBodyHtml = mm.IsBodyHtml;
                dn.Body = mm.Body;
            }
            else
            {
                var bestView = mm.AlternateViews
                    .OrderByDescending(a => a.ContentType.MediaType.Contains("htm"))
                    .ThenByDescending(a => a.ContentStream.Length)
                    .FirstOrDefault();

                if (bestView != null)
                {
                    dn.IsBodyHtml = bestView.ContentType.MediaType.Contains("htm");
                    string body = 

                    dn.Body = MailMimeParser.GetStringFromStream(bestView.ContentStream, bestView.ContentType);
                    dn.Attachments.AddRange(bestView.LinkedResources.Select(a => 
                        new EmailAttachmentDN
                        {
                            ContentId = a.ContentId,
                            File = new FilePathDN(EmailFileType.Attachment, a.ContentType.Name, a.ContentStream.ReadAllBytes()).Save(),
                            Type = EmailAttachmentType.LinkedResource
                        }));
                }
            }

            if (dn.Attachments.Any())
            {
                dn.Body = Regex.Replace(dn.Body, "src=\"(?<link>[^\"]*)\"", m =>
                {
                    var value = m.Groups["link"].Value;

                    if (!value.StartsWith("cid:"))
                        return m.Value;

                    value = value.After("cid:");

                    var link = dn.Attachments.Where(a => a.ContentId == value).Select(a => a.File.FullWebPath).FirstOrDefault() ?? "http://missing/missing.jpg";

                    return "src=\"{0}\"".Formato(link);
                });
            }

            return dn;
        }
    }
}
