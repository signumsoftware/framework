using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Utilities;
using Signum.Entities.Mailing;
using Signum.Engine.Processes;
using Signum.Entities.Processes;
using Signum.Entities;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Operations;
using Signum.Engine.Operations;
using Signum.Engine.Extensions.Properties;
using System.Net;
using Signum.Engine.Authorization;
using Signum.Utilities.Reflection;
using System.ComponentModel;
using System.Web;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Linq.Expressions;
using Signum.Entities.Exceptions;
using Signum.Engine.Exceptions;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using System.Text.RegularExpressions;

namespace Signum.Engine.Mailing
{
    public class EmailContent
    {
        public string Subject { get; set; }
        public string Body { get; set; }
        bool IsPlainText { get; set; }
    }

    public interface IEmailModel
    {
        IEmailOwnerDN To { get; set; }
        string Cc { get; set; }
        string Bcc { get; set; }
    }

    public class EmailModel<T> : IEmailModel
        where T : IEmailOwnerDN
    {
        public T To;

        IEmailOwnerDN IEmailModel.To
        {
            get { return To; }
            set { To = (T)value; }
        }

        public string Cc { get; set; }
        public string Bcc { get; set; }
    }

    public static class EmailLogic
    {
        public static string DoNotSend = "null@null.com";

        public static Func<string> OverrideEmailAddress = () => null;

        public static Func<EmailMessageDN, SmtpClient> SmtpClientBuilder;

        static Dictionary<Type, Func<IEmailModel, EmailContent>> templates = new Dictionary<Type, Func<IEmailModel, EmailContent>>();
        static Dictionary<Type, Lite<EmailTemplateOldDN>> templateToDN;

        internal static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => EmailLogic.Start(null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<EmailMessageDN>();

                dqm[typeof(EmailTemplateOldDN)] = (from e in Database.Query<EmailTemplateOldDN>()
                                                   select new
                                                   {
                                                       Entity = e,
                                                       e.Id,
                                                       e.FullClassName,
                                                       e.FriendlyName,
                                                   }).ToDynamic();

                dqm[typeof(EmailMessageDN)] = (from e in Database.Query<EmailMessageDN>()
                                               select new
                                               {
                                                   Entity = e,
                                                   e.Id,
                                                   e.Recipient,
                                                   e.State,
                                                   e.Subject,
                                                   e.Body,
                                                   Template = e.TemplateOld,
                                                   e.Sent,
                                                   e.Received,
                                                   e.Package,
                                                   e.Exception,
                                               }).ToDynamic();

                sb.Schema.Initializing[InitLevel.Level2NormalEntities] += Schema_Initializing;
                sb.Schema.Generating += Schema_Generating;
                sb.Schema.Synchronizing += Schema_Synchronizing;
            }
        }

        #region database management
        static void Schema_Initializing()
        {
            List<EmailTemplateOldDN> dbTemplates = Database.RetrieveAll<EmailTemplateOldDN>();

            templateToDN = EnumerableExtensions.JoinStrict(
                dbTemplates, templates.Keys, t => t.FullClassName, t => t.FullName,
                (typeDN, type) => new { typeDN, type }, "caching EmailTemplates").ToDictionary(a => a.type, a => a.typeDN.ToLite());
        }

        static readonly string EmailTemplates = "EmailTemplates";


        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            Table table = Schema.Current.Table<EmailTemplateOldDN>();

            Dictionary<string, EmailTemplateOldDN> should = GenerateTemplates().ToDictionary(s => s.FullClassName);
            Dictionary<string, EmailTemplateOldDN> old = Administrator.TryRetrieveAll<EmailTemplateOldDN>(replacements).ToDictionary(c => c.FullClassName);

            replacements.AskForReplacements(old, should, EmailTemplates);

            Dictionary<string, EmailTemplateOldDN> current = replacements.ApplyReplacements(old, EmailTemplates);

            return Synchronizer.SynchronizeScript(should, current, 
                (tn, s) => table.InsertSqlSync(s), 
                (tn, c) => table.DeleteSqlSync(c), 
                (tn, s, c) =>
                {
                    c.FullClassName = s.FullClassName;
                    c.FriendlyName = s.FriendlyName;
                    return table.UpdateSqlSync(c);
                }, 
                Spacing.Double);
        }

        static SqlPreCommand Schema_Generating()
        {
            Table table = Schema.Current.Table<EmailTemplateOldDN>();

            return (from ei in GenerateTemplates()
                    select table.InsertSqlSync(ei)).Combine(Spacing.Simple);
        }

        internal static List<EmailTemplateOldDN> GenerateTemplates()
        {
            var lista = (from type in templates.Keys
                         select new EmailTemplateOldDN
                         {
                             FullClassName = type.FullName,
                             FriendlyName = type.NiceName()
                         }).ToList();
            return lista;
        }
        #endregion

        public static void StarProcesses(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<EmailPackageDN>();

                ProcessLogic.AssertStarted(sb);
                ProcessLogic.Register(EmailProcesses.SendEmails, new SendEmailProcessAlgorithm());

                new BasicConstructFromMany<EmailMessageDN, ProcessExecutionDN>(EmailOperations.ReSendEmails)
                {
                    Construct = (messages, args) => ProcessLogic.Create(EmailProcesses.SendEmails, messages)
                }.Register();

                dqm[typeof(EmailPackageDN)] = (from e in Database.Query<EmailPackageDN>()
                                               select new
                                               {
                                                   Entity = e,
                                                   e.Id,
                                                   e.Name,
                                                   e.NumLines,
                                                   e.NumErrors,
                                               }).ToDynamic();
            }
        }

        public static void RegisterTemplate<T>(Func<T, EmailContent> template)
            where T : IEmailModel
        {
            templates[typeof(T)] = et => template((T)et);
        }

        public static Lite<EmailTemplateOldDN> GetTemplateDN(Type type)
        {
            return templateToDN.GetOrThrow(type, "{0} not registered in EmailLogic");
        }

        public static Func<IEmailModel, EmailContent> GetTemplate(Type type)
        {
            return templates.GetOrThrow(type, "{0} not registered in EmailLogic");
        }

        public static EmailMessageDN CreateEmailMessage(IEmailModel model, Lite<EmailPackageDN> package)
        {

            if (model == null)
                throw new ArgumentNullException("model");

            if (model.To == null)
                throw new ArgumentNullException("model.To");


            using (Sync.ChangeBothCultures(model.To.CultureInfo))
            {
                EmailContent content = GetTemplate(model.GetType())(model);

                var result = new EmailMessageDN
                {
                    State = EmailState.Created,
                    Recipient = model.To.ToLite(),
                    Bcc = model.Bcc,
                    Cc = model.Cc,
                    TemplateOld = GetTemplateDN(model.GetType()),
                    Subject = content.Subject,
                    Body = content.Body,
                    Package = package
                };
                return result;
            }
        }

        public static EmailMessageDN Send(this IEmailModel model)
        {
            EmailMessageDN result = CreateEmailMessage(model, null);

            SendMail(result);

            return result;
        }

        public static void SendMail(EmailMessageDN emailMessage)
        {
            try
            {
                MailMessage message = CreateMailMessage(emailMessage);

                if (message != null)
                {
                    SmtpClient client = SmtpClientBuilder == null ? SafeSmtpClient() : SmtpClientBuilder(emailMessage);
                    client.Send(message);
                }

                emailMessage.State = EmailState.Sent;
                emailMessage.Sent = TimeZoneManager.Now;
                emailMessage.Received = null;
                emailMessage.Save();
            }
            catch (Exception e)
            {
                if (Transaction.AvoidIndependentTransactions)
                    throw;

                var exLog = e.LogException().ToLite();

                using (Transaction tr = Transaction.ForceNew())
                {
                    emailMessage.Exception = exLog;
                    emailMessage.State = EmailState.SentError;
                    emailMessage.Save();
                    tr.Commit();
                }

                throw;
            }
        }

        public static SmtpClient SafeSmtpClient()
        {
            //http://weblogs.asp.net/stanleygu/archive/2010/03/31/tip-14-solve-smtpclient-issues-of-delayed-email-and-high-cpu-usage.aspx
            return new SmtpClient()
            {
                ServicePoint = { MaxIdleTime = 2 }
            };
        }

        internal static SmtpClient SafeSmtpClient(string host, int port)
        {
            //http://weblogs.asp.net/stanleygu/archive/2010/03/31/tip-14-solve-smtpclient-issues-of-delayed-email-and-high-cpu-usage.aspx
            return new SmtpClient(host, port)
            {
                ServicePoint = { MaxIdleTime = 2 }
            };
        }

        public static void SendAsync(this IEmailModel model)
        {
            EmailMessageDN message = CreateEmailMessage(model, null);

            SendMailAsync(message);
        }

        class EmailUser
        {
            public EmailMessageDN EmailMessage;
            public UserDN User;
        }

        public static void SendMailAsync(EmailMessageDN emailMessage)
        {
            try
            {
                MailMessage message = CreateMailMessage(emailMessage);
                if (message != null)
                {
                    SmtpClient client = SmtpClientBuilder == null ? SafeSmtpClient() : SmtpClientBuilder(emailMessage);

                    emailMessage.Sent = null;
                    emailMessage.Received = null;
                    emailMessage.Save();

                    client.SafeSendMailAsync(message, args =>
                    {
                        Expression<Func<EmailMessageDN, EmailMessageDN>> updater;
                        if (args.Error != null)
                        {
                            var exLog = args.Error.LogException().ToLite();
                            updater = em => new EmailMessageDN
                            {
                                Exception = exLog,
                                State = EmailState.SentError
                            };
                        }
                        else
                            updater = em => new EmailMessageDN
                            {
                                State = EmailState.Sent,
                                Sent = TimeZoneManager.Now
                            };

                        for (int i = 0; i < 4; i++)
                        {
                            if (emailMessage.InDB().UnsafeUpdate(updater) > 0)
                                return;

                            if (i != 3)
                                Thread.Sleep(3000);
                        }
                    }); 

                    client.SendAsync(message, new EmailUser { EmailMessage = emailMessage, User = UserDN.Current });
                }
                else
                {
                    emailMessage.Received = null;
                    emailMessage.State = EmailState.Sent;
                    emailMessage.Sent = TimeZoneManager.Now;
                    emailMessage.Save();
                }
            }
            catch (Exception ex)
            {
                if (Transaction.AvoidIndependentTransactions)
                    throw;

                var exLog = ex.LogException().ToLite();

                using (var tr = Transaction.ForceNew())
                {
                    emailMessage.Sent = TimeZoneManager.Now;
                    emailMessage.State = EmailState.SentError;
                    emailMessage.Exception = exLog;
                    emailMessage.Save();
                    tr.Commit();
                }
            }
        }

        public static void SafeSendMailAsync(this SmtpClient client, MailMessage message, Action<AsyncCompletedEventArgs> onComplete)
        {
            client.SendCompleted += (object sender, AsyncCompletedEventArgs e) =>
            {
                client.Dispose();
                message.Dispose();
                using (AuthLogic.Disable())
                {
                    try
                    {
                        onComplete(e);
                    }
                    catch (Exception ex)
                    {
                        ex.LogException();
                    }
                }
            };
            client.SendAsync(message, null);
        }

        static MailMessage CreateMailMessage(EmailMessageDN emailMessage)
        {
            var address = OverrideEmailAddress();

            if (address == DoNotSend)
                return null;

            MailMessage message = new MailMessage()
            {
                To = { address ?? emailMessage.Recipient.Retrieve().Email },
                Subject = emailMessage.Subject,
                Body = emailMessage.Body,
                IsBodyHtml = true,
            };

            if (emailMessage.Bcc.HasText())
                message.Bcc.AddRange(emailMessage.Bcc.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(a => new MailAddress(a)).ToList());
            if (emailMessage.Cc.HasText())
                message.CC.AddRange(emailMessage.Cc.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(a => new MailAddress(a)).ToList());
            return message;
        }

        public static ProcessExecutionDN SendAll<T>(List<T> emails)
            where T : IEmailModel
        {
            EmailPackageDN package = new EmailPackageDN
            {
                NumLines = emails.Count
            }.Save();

            var packLite = package.ToLite();

            emails.Select(e => CreateEmailMessage(e, packLite)).SaveList();

            var process = ProcessLogic.Create(EmailProcesses.SendEmails, package);

            process.Execute(ProcessOperation.Execute);

            return process;
        }

        public static ProcessExecutionDN SendToMany<T>(EmailModel<T> model, List<T> recipientList)
            where T : class, IEmailOwnerDN
        {
            if (model.To != null)
                throw new InvalidOperationException("model should have no To");

            EmailContent content = GetTemplate(model.GetType())(model);
            var template = GetTemplateDN(model.GetType());

            EmailPackageDN package = new EmailPackageDN
            {
                NumLines = recipientList.Count,
            }.Save();

            var lite = package.ToLite();

            recipientList.Select(to => new EmailMessageDN
            {
                State = EmailState.Created,
                Recipient = to.ToLite<IEmailOwnerDN>(),
                TemplateOld = template,
                Subject = content.Subject,
                Body = content.Body,
                Package = lite
            }).SaveList();

            var process = ProcessLogic.Create(EmailProcesses.SendEmails, package);

            process.Execute(ProcessOperation.Execute);

            return process;
        }

        public static Dictionary<Type, Exception> GetAllErrors()
        {
            Dictionary<Type, Exception> exceptions = new Dictionary<Type, Exception>();

            foreach (var item in templates)
            {
                try
                {
                    item.Value((IEmailModel)Activator.CreateInstance(item.Key));
                }
                catch (Exception e)
                {
                    exceptions.Add(item.Key, e);
                }
            }

            return exceptions;
        }


        #region nuevo
        public static void Start2()
        {
            EmailTemplateDN.AssociatedTypeIsEmailOwner = t => t.ToType() is IEmailOwnerDN;

        }

        public static EmailSenderManager SenderManager = new EmailSenderManager();

        static HashSet<EnumDN> SystemTemplates = new HashSet<EnumDN>();

        public static void RegisterSystemTemplate(EnumDN[] systemTemplates)
        {
            foreach (var st in systemTemplates)
            {
                if (!SystemTemplates.Contains(st))
                    SystemTemplates.Add(st);
            }
        }

        public static void UnregisterSystemTemplate(EnumDN systemTemplate)
        {
            if (SystemTemplates.Contains(systemTemplate))
                SystemTemplates.Remove(systemTemplate);
        }

        public static EmailMessageDN CreateEmailMessage(this Entity entity, EnumDN systemTemplate)
        {
            var template = Database.Query<EmailTemplateDN>().SingleEx(t => t.IsActiveNow() == true && t.SystemTemplate == systemTemplate);
            return CreateEmailMessage(entity, template);
        }

        public static EmailMessageDN CreateEmailMessage(this IIdentifiable entity, EmailTemplateDN template)
        {
            Type query = template.AssociatedType.GetType();
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(query);

            var columns = template.Replacements.Select(qt => new Column(QueryUtils.Parse("Entity." + qt.TokenString, qd), null)).ToList();

            if (template.Recipient != null)
            {
                columns.Insert(0, new Column(QueryUtils.Parse("Entity." + template.Recipient.TokenString, qd), null));
            }

            var entityToken = QueryUtils.Parse("Entity", qd);

            var table = DynamicQueryManager.Current.ExecuteQuery(new QueryRequest
            {
                QueryName = query,
                Columns = columns,
                ElementsPerPage = QueryRequest.AllElements,
                Filters = new List<Filter>
                {
                    new Filter
                    { 
                        Token = entityToken,
                        Operation = FilterOperation.EqualTo,
                        Value = Lite.Create(entityToken.Type.CleanType(), entity)
                    }
                }
            });

            var email = new EmailMessageDN
            {
                Recipient = template.Recipient == null
                    ? (entity as IEmailOwnerDN).ToLite()
                    : ((Lite)table.Rows.DistinctSingle(table.Columns[0])).ToLite<IEmailOwnerDN>(),
                From = template.From,
                DisplayFrom = template.DisplayFrom,
                Cc = template.Cc,
                Bcc = template.Bcc,
                IsBodyHtml = template.IsBodyHtml,
                EditableMessage = template.EditableMessage,
                Template = template.ToLite(),
            };

            var tokensString = TokenRegex.Matches(template.Text).Cast<Match>().Select(m => m.Groups["token"].Value).ToList();
            tokensString.AddRange(TokenRegex.Matches(template.Subject).Cast<Match>().Select(m => m.Groups["token"].Value).ToList());
            var tokens = tokensString.Select(t => QueryUtils.Parse(t, qd)).Distinct().ToList();
            var dicTokenColumn = tokens.Select((t, i) => KVP.Create(t.FullKey(), i)).ToDictionary();

            email.Subject = ComposeText(template.Subject, table, table.Rows, dicTokenColumn, false);
            var text = ComposeText(template.Text, table, table.Rows, dicTokenColumn, template.IsBodyHtml);

            if (template.MasterTemplate != null)
                text = MasterTemplateContentRegex.Replace(template.MasterTemplate.Retrieve().Text, m => text);

            email.Body = text;

            return email;
        }

        static object DistinctSingle(this IEnumerable<ResultRow> rows, ResultColumn column)
        {
            return rows.Select(r => r[column]).Distinct().SingleEx(() =>
                "Multiple values for column {0}".Formato(column.Column.Token.FullKey()));
        }

        public static readonly Regex TokenRegex = new Regex(@"\@(?<special>(foreach|endforeach|))\[(?<token>[^\]]*)\]");

        internal class ReplacementIreration
        {
            public string Token;
           
            public int Index;
            public int Length;
            public List<ReplacementIreration> SubIterations;
        }



        //public static readonly Regex TokenRegex = new Regex(@"\@\[(?<token>[^\]]*)\]");
        public static readonly Regex StartIterationRegex = new Regex(@"\@foreach\[(?<token>[^\]]*)\]");
        public static readonly Regex EndIterationRegex = new Regex(@"\@endforeach\[(?<token>[^\]]*)\]");
        public static readonly Regex MasterTemplateContentRegex = new Regex(@"\@\[content\]");


        private static string ComposeText(string text, ResultTable table, IEnumerable<ResultRow> rows,
            Dictionary<string, int> dicTokenColumn, bool isHtml)
        {
            text = ProcessIterations(text, table, rows, dicTokenColumn, isHtml);

            TokenRegex.Replace(text, m =>
            {
                var index = dicTokenColumn[m.Groups["token"].Value];
                var value = table.Rows[index].TryToString();
                if (isHtml)
                    return HttpUtility.HtmlEncode(value);
                else
                    return value;
            });

            return text;
        }

        static string ProcessIterations(string text, ResultTable table, IEnumerable<ResultRow> rows,
            Dictionary<string, int> dicTokenColumn, bool isHtml)
        {
            Match matchStart;
            while ((matchStart = StartIterationRegex.Match(text)).Success)
            {
                var tokenName = matchStart.Groups["token"].Value;

                var matchesEnd = EndIterationRegex.Matches(text, matchStart.Index + matchStart.Length).Cast<Match>().Where(m =>
                    m.Groups["token"].Value == tokenName).OrderBy(m => m.Index);

                if (!matchesEnd.Any())
                    throw new FormatException(Resources.IterationForToken0IsNotClosed.Formato(tokenName));

                int indexSearch = matchStart.Index + matchStart.Length;
                Match matchEnd = null;
                foreach (var match in matchesEnd)
                {
                    if (StartIterationRegex.Matches(text.Substring(indexSearch, match.Index - indexSearch)).Cast<Match>().Any(m =>
                        m.Groups["token"].Value == tokenName))
                    {
                        indexSearch = match.Index + match.Length;
                    }
                    else
                    {
                        matchEnd = match;
                        break;
                    }
                }

                if (matchEnd == null)
                    throw new FormatException(Resources.IterationForToken0IsNotClosed.Formato(tokenName));

                string beforeIterationText = text.Left(matchStart.Index);
                string iterationText = text.Substring(matchStart.Index + matchStart.Length, matchStart.Index - 1);
                string afterIterationText = text.Right(text.Length - (matchEnd.Index + matchEnd.Length));

                StringBuilder sb = new StringBuilder();
                sb.Append(beforeIterationText);
                var groupedRows = rows.GroupBy(r => r[dicTokenColumn[tokenName]]);
                foreach (var gr in groupedRows)
                {
                    sb.Append(ComposeText(iterationText, table, gr, dicTokenColumn, isHtml));
                }
                sb.Append(afterIterationText);

                text = sb.ToString();
            }
            return text;
        }

        #endregion
    }

    public struct Link
    {
        public readonly string Url;
        public readonly string Content;

        public Link(string url, string content)
        {
            this.Url = url;
            this.Content = content;
        }

        public override string ToString()
        {
            return @"<a href='{0}'>{1}</a>".Formato(Url, HttpUtility.HtmlEncode(Content));
        }

    }


    public virtual class EmailSenderManager
    {
        MailMessage CreateMailMessage(EmailMessageDN email)
        {
            email.To = GetRecipient(email, null);

            MailMessage message = new MailMessage()
            {
                To = { new MailAddress(email.To) },
                From = email.DisplayFrom.HasText() ? new MailAddress(email.From, email.DisplayFrom) : new MailAddress(email.From),
                Subject = email.Subject,
                Body = email.Body,
                IsBodyHtml = email.IsBodyHtml,
            };

            if (email.Bcc.HasText())
                message.Bcc.AddRange(email.Bcc.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(a => new MailAddress(a)).ToList());
            if (email.Cc.HasText())
                message.CC.AddRange(email.Cc.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(a => new MailAddress(a)).ToList());
            return message;
        }

        internal virtual string GetRecipient(EmailMessageDN email, object[] args)
        {
            return email.Recipient.InDB().Select(r => r.Email).SingleEx();
        }

        internal virtual void Send(EmailMessageDN email)
        {
            try
            {
                SmtpClient client = CreateSmtpClient(email);

                MailMessage message = CreateMailMessage(email);

                client.Send(message);

                email.State = EmailState.Sent;
                email.Sent = TimeZoneManager.Now;
                email.Received = null;
                email.Save();
            }
            catch (Exception ex)
            {
                if (Transaction.AvoidIndependentTransactions)
                    throw;

                var exLog = ex.LogException().ToLite();

                using (Transaction tr = Transaction.ForceNew())
                {
                    email.Exception = exLog;
                    email.State = EmailState.SentError;
                    email.Save();
                    tr.Commit();
                }

                throw;
            }
        }

        SmtpClient CreateSmtpClient(EmailMessageDN email)
        {
            return email.Template != null
                ? email.Template.InDB().Select(t => t.SMTPConfiguration).SingleOrDefault().TryCC(c => c.GenerateSmtpClient(true))
                : null
                ?? EmailLogic.SafeSmtpClient();
        }

        class EmailUser
        {
            public EmailMessageDN EmailMessage;
            public UserDN User;
        }

        internal virtual void SendAsync(EmailMessageDN email)
        {
            try
            {
                SmtpClient client = CreateSmtpClient(email);
                client.SendCompleted += new SendCompletedEventHandler(SendCompleted);

                MailMessage message = CreateMailMessage(email);

                email.Sent = null;
                email.Received = null;
                email.Save();

                client.SendAsync(message, new EmailUser { EmailMessage = email, User = UserDN.Current });
            }
            catch (Exception ex)
            {
                if (Transaction.AvoidIndependentTransactions)
                    throw;

                var exLog = ex.LogException().ToLite();

                using (Transaction tr = Transaction.ForceNew())
                {
                    email.Exception = exLog;
                    email.State = EmailState.SentError;
                    email.Save();
                    tr.Commit();
                }

                throw;
            }
        }

        internal virtual void SendCompleted(object sender, AsyncCompletedEventArgs e)
        {
            EmailUser emailUser = (EmailUser)e.UserState;
            using (AuthLogic.UserSession(emailUser.User))
            {
                Expression<Func<EmailMessageDN, EmailMessageDN>> updater;
                if (e.Error != null)
                {
                    var exLog = e.Error.LogException().ToLite();
                    updater = em => new EmailMessageDN
                    {
                        Exception = exLog,
                        State = EmailState.SentError
                    };
                }
                else
                    updater = em => new EmailMessageDN
                    {
                        State = EmailState.Sent,
                        Sent = TimeZoneManager.Now
                    };

                //to allow main thread to insert the emailMessageDN into DB
                for (int i = 0; i < 4; i++)
                {
                    if (emailUser.EmailMessage.InDB().UnsafeUpdate(updater) > 0)
                        return;

                    if (i != 3)
                        Thread.Sleep(3000);
                }
            }
        }
    }


}
