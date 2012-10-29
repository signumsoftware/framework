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
using System.Globalization;

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

        internal static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => EmailLogic.Start(null, null, null, null, null, null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, TemplateCultures cultures, string urlPrefix, 
            string defaultFrom, string defaultDisplayFrom, string defaultBcc)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<EmailMessageDN>();
                sb.Include<EmailTemplateDN>();
                sb.Include<EmailMasterTemplateDN>();

                dqm[typeof(EmailMasterTemplateDN)] = (from t in Database.Query<EmailMasterTemplateDN>()
                                                      select new
                                                      {
                                                          Entity = t,
                                                          t.Id,
                                                          t.Name,
                                                          t.State
                                                      }).ToDynamic();

                dqm[typeof(EmailTemplateDN)] = (from t in Database.Query<EmailTemplateDN>()
                                                select new
                                                {
                                                    Entity = t,
                                                    t.Id,
                                                    t.Name,
                                                    t.From,
                                                    t.DisplayFrom,
                                                    t.Bcc,
                                                    t.Cc,
                                                    Active = t.IsActiveNow(),
                                                    t.IsBodyHtml
                                                }).ToDynamic();

                dqm[typeof(EmailMessageDN)] = (from e in Database.Query<EmailMessageDN>()
                                               select new
                                               {
                                                   Entity = e,
                                                   e.Id,
                                                   e.Recipient,
                                                   e.State,
                                                   e.Subject,
                                                   Body = e.Text,
                                                   //Template = e.TemplateOld,
                                                   e.Sent,
                                                   e.Received,
                                                   e.Package,
                                                   e.Exception,
                                               }).ToDynamic();

                sb.Schema.Generating += Schema_Generating;
                sb.Schema.Synchronizing += Schema_Synchronizing;

                sb.Schema.EntityEvents<EmailTemplateDN>().PreSaving += new PreSavingEventHandler<EmailTemplateDN>(EmailTemplate_PreSaving);

                EmailTemplateGraph.Register();
                EmailMasterTemplateGraph.Register();

                EmailTemplateDN.AssociatedTypeIsEmailOwner = t =>
                    typeof(IEmailOwnerDN).IsAssignableFrom(t.ToType());

                EnumLogic<SystemTemplateDN>.Start(sb, () => systemTemplates.Keys.ToHashSet());

                EmailCultures = cultures;

                SenderManager = new EmailSenderManager(urlPrefix, defaultFrom, defaultDisplayFrom, defaultBcc);
            }
        }

        public static TemplateCultures EmailCultures;

        static Dictionary<Enum, Func<TemplateCultures, EmailTemplateDN>> systemTemplates = 
            new Dictionary<Enum, Func<TemplateCultures, EmailTemplateDN>>();

        public static void RegisterSystemTemplate(Enum systemTemplate, Func<TemplateCultures, EmailTemplateDN> defaultTemplateConstructor = null)
        {
            systemTemplates[systemTemplate] = defaultTemplateConstructor;
        }

        static void EmailTemplate_PreSaving(EmailTemplateDN template, ref bool graphModified)
        {
            Type query = template.AssociatedType.ToType();
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(query);

            var tokensString = template.Messages.SelectMany(tm =>
                TokenRegex.Matches(tm.Text).Cast<Match>().Where(m => 
                    m.Groups["global"].Length == 0).Select(m => m.Groups["token"].Value)).ToList();
            tokensString.AddRange(template.Messages.SelectMany(tm =>
                TokenRegex.Matches(tm.Subject).Cast<Match>().Where(m => 
                    m.Groups["global"].Length == 0).Select(m => m.Groups["token"].Value)).ToList());
            var tokens = tokensString.Select(t => QueryUtils.Parse(t, qd)).Distinct().ToList();

            var tokensRemoved = template.Tokens.TryCC(tt => tt.Extract(t => !tokens.Contains(t.Token))) ?? new List<TemplateQueryTokenDN>();

            var tokensToAdd = tokens.Where(t =>
                !template.Tokens.Any(tt => tt.Token == t))
                .Select(t => new TemplateQueryTokenDN { Token = t });

            if (tokensRemoved.Any() || tokensToAdd.Any())
            {
                if (template.Tokens == null)
                    template.Tokens = new MList<TemplateQueryTokenDN>();
                template.Tokens.AddRange(tokensToAdd);
                graphModified = true;
            }
        }

        #region database management
        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            return SqlPrecommandSystemTemplates();
        }

        static SqlPreCommand Schema_Generating()
        {
            return SqlPrecommandSystemTemplates();
        }

        private static SqlPreCommand SqlPrecommandSystemTemplates()
        {
            var presentTemplates = Database.Query<EmailTemplateDN>().Where(et =>
                et.SystemTemplate != null).Select(et => et.SystemTemplate).Distinct().ToHashSet();

            List<EmailTemplateDN> should = systemTemplates.Where(kvp =>
                kvp.Value != null && !presentTemplates.Any(st => st.Is(EnumLogic<SystemTemplateDN>.ToEntity(kvp.Key))))
                .Select(kvp => 
                { 
                    var template = kvp.Value(EmailLogic.EmailCultures);
                    template.SystemTemplate = EnumLogic<SystemTemplateDN>.ToEntity(kvp.Key);
                    return template;
                }).ToList();

            if (!should.Any())
                return null;

            Table table = Schema.Current.Table<EmailTemplateDN>();

            return SqlPreCommand.Combine(Spacing.Double, should.Select(s => table.InsertSqlSync(s)).ToArray());
        }

        #endregion

        #region Old

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

        public static EmailMessageDN CreateEmailMessage(IEmailModel model, Lite<EmailPackageDN> package)
        {

            if (model == null)
                throw new ArgumentNullException("model");

            if (model.To == null)
                throw new ArgumentNullException("model.To");


            using (Sync.ChangeBothCultures(model.To.CultureInfo))
            {
                //EmailContent content = GetTemplate(model.GetType())(model);

                var result = new EmailMessageDN
                {
                    State = EmailState.Created,
                    Recipient = model.To.ToLite(),
                    Bcc = model.Bcc,
                    Cc = model.Cc,
                    //TemplateOld = GetTemplateDN(model.GetType()),
                    //Subject = content.Subject,
                    //Text = content.Body,
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
                Body = emailMessage.Text,
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

            //EmailContent content = GetTemplate(model.GetType())(model);
            //var template = GetTemplateDN(model.GetType());

            EmailPackageDN package = new EmailPackageDN
            {
                NumLines = recipientList.Count,
            }.Save();

            var lite = package.ToLite();

            recipientList.Select(to => new EmailMessageDN
            {
                State = EmailState.Created,
                Recipient = to.ToLite<IEmailOwnerDN>(),
                //TemplateOld = template,
                //Subject = content.Subject,
                //Text = content.Body,
                Package = lite
            }).SaveList();

            var process = ProcessLogic.Create(EmailProcesses.SendEmails, package);

            process.Execute(ProcessOperation.Execute);

            return process;
        }

        public static Dictionary<Type, Exception> GetAllErrors()
        {
            Dictionary<Type, Exception> exceptions = new Dictionary<Type, Exception>();

            //foreach (var item in templates)
            //{
            //    try
            //    {
            //        item.Value((IEmailModel)Activator.CreateInstance(item.Key));
            //    }
            //    catch (Exception e)
            //    {
            //        exceptions.Add(item.Key, e);
            //    }
            //}

            return exceptions;
        }

        #endregion

        public static EmailSenderManager SenderManager;

        public static EmailMessageDN CreateEmailMessage(this IIdentifiable entity, SystemTemplateDN systemTemplate)
        {
            var template = Database.Query<EmailTemplateDN>().SingleEx(t =>
                t.IsActiveNow() == true &&
                t.SystemTemplate == systemTemplate);
            return CreateEmailMessage(entity, template);
        }

        public static EmailMessageDN CreateEmailMessage(this IIdentifiable entity, EmailTemplateDN template)
        {
            Type query = template.AssociatedType.ToType();
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(query);

            var columns = template.Tokens.Select(qt => new Column(QueryUtils.Parse("Entity." + qt.TokenString, qd), null)).ToList();

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

            var recipient = template.Recipient == null
                    ? (entity as IEmailOwnerDN).ToLite()
                    : ((Lite)table.Rows.DistinctSingle(table.Columns[0])).ToLite<IEmailOwnerDN>();

            var email = new EmailMessageDN
            {
                Recipient = recipient,
                From = template.From,
                DisplayFrom = template.DisplayFrom,
                Cc = template.Cc,
                Bcc = template.Bcc,
                IsBodyHtml = template.IsBodyHtml,
                EditableMessage = template.EditableMessage,
                Template = template.ToLite(),
            };

            var dicTokenColumn = table.Columns.ToDictionary(rc => rc.Column.Token.FullKey().Substring("Entity.".Length));

            var recipientCI = recipient.InDB().Select(io => io.CultureInfo).SingleOrDefault();
            var cultureInfo = recipientCI.HasText() ? CultureInfo.GetCultureInfo(recipientCI) : CultureInfo.InvariantCulture;

            var message = template.Messages.SingleOrDefault(tm => tm.GetCultureInfo == cultureInfo) ??
                template.Messages.SingleOrDefault(tm => tm.GetCultureInfo == cultureInfo.Parent) ??
                template.Messages.SingleOrDefault(tm => tm.GetCultureInfo == CultureInfo.InvariantCulture);

            email.Subject = ParseTemplate(message.Subject).Print(table.Rows, dicTokenColumn, false, cultureInfo, entity);
            var body = ParseTemplate(message.Text).Print(table.Rows, dicTokenColumn, template.IsBodyHtml, cultureInfo, entity);

            if (template.MasterTemplate != null)
                body = EmailMasterTemplateDN.MasterTemplateContentRegex.Replace(template.MasterTemplate.Retrieve().Text, m => body);

            email.Text = body;

            return email;
        }

        #region Compose email message

        static object DistinctSingle(this IEnumerable<ResultRow> rows, ResultColumn column)
        {
            return rows.Select(r => r[column]).Distinct().SingleEx(() =>
                "Multiple values for column {0}".Formato(column.Column.Token.FullKey()));
        }

        public static readonly Regex TokenRegex = new Regex(@"\@(?<special>(foreach|endforeach|global|))\[(?<token>[^\]]*)\]");

        abstract class TextNode
        {
            public abstract void PrintList(StringBuilder sb, IEnumerable<ResultRow> rows, Dictionary<string, ResultColumn> dic, 
                bool isHtml, CultureInfo ci, IIdentifiable entity);
        }

        class LiteralNode : TextNode
        {
            public string Text;

            public override void PrintList(StringBuilder sb, IEnumerable<ResultRow> rows, Dictionary<string, ResultColumn> dic,
                bool isHtml, CultureInfo ci, IIdentifiable entity)
            {
                sb.Append(Text);
            }
        }

        class TokenNode : TextNode
        {
            public string Token;

            public override void PrintList(StringBuilder sb, IEnumerable<ResultRow> rows, Dictionary<string, ResultColumn> dic, 
                bool isHtml, CultureInfo ci, IIdentifiable entity)
            {
                var column = dic[Token];
                object obj = rows.DistinctSingle(dic[Token]);
                var text = obj is IFormattable ? ((IFormattable)obj).ToString(column.Column.Token.Format, ci) : obj.TryToString();
                sb.Append(isHtml ? HttpUtility.HtmlEncode(text) : text);
            }
        }

        class GlobalNode : TextNode
        {
            public string Token;

            public override void PrintList(StringBuilder sb, IEnumerable<ResultRow> rows, Dictionary<string, ResultColumn> dic, 
                bool isHtml, CultureInfo ci, IIdentifiable entity)
            {
                var tokenFunc = SenderManager.GlobalTokens.TryCC(ct => ct.TryGetC(Token));
                if (tokenFunc == null)
                    throw new ArgumentException("The global token {0} was not found".Formato(Token));
                var text = tokenFunc(new GlobalDispatcher { Entity = entity, Culture = ci, IsHtml = isHtml });
                sb.Append(text);
            }
        }

        class IterationNode : TextNode
        {
            public string Token;
            public List<TextNode> Nodes = new List<TextNode>();

            public string Print(IEnumerable<ResultRow> rows, Dictionary<string, ResultColumn> dic, bool isHtml, CultureInfo ci, IIdentifiable entity)
            {
                StringBuilder sb = new StringBuilder();
                this.PrintList(sb, rows, dic, isHtml, ci, entity);
                return sb.ToString();
            }

            public override void PrintList(StringBuilder sb, IEnumerable<ResultRow> rows, Dictionary<string, ResultColumn> dic,
                bool isHtml, CultureInfo ci, IIdentifiable entity)
            {
                if (Token == null)
                    foreach (var node in Nodes)
                    {
                        node.PrintList(sb, rows, dic, isHtml, ci, entity);
                    }
                else
                {
                    var groups = rows.GroupBy(r => rows.DistinctSingle(dic[Token])).ToList();
                    if (groups.Count == 1 && groups[0].Key == null)
                        return;
                    foreach (var group in groups)
                    {
                        foreach (var node in Nodes)
                        {
                            node.PrintList(sb, group, dic, isHtml, ci, entity);
                        }
                    }
                }
            }
        }

        static IterationNode ParseTemplate(string text)
        {
            IterationNode node;
            var error = TryParseTemplate(text, out node);
            if (error.HasText())
                throw new FormatException(error);
            return node;
        }

        static string TryParseTemplate(string text, out IterationNode iterationNode)
        {
            var matches = TokenRegex.Matches(text);

            Stack<IterationNode> stack = new Stack<IterationNode>();
            iterationNode = new IterationNode();
            stack.Push(iterationNode);

            int index = 0;
            foreach (Match match in matches)
            {
                if (index < match.Index)
                {
                    stack.Peek().Nodes.Add(new LiteralNode { Text = text.Substring(index, match.Index - index) });
                }
                var token = match.Groups["token"].Value;
                switch (match.Groups["special"].Value)
                {
                    case "":
                        stack.Peek().Nodes.Add(new TokenNode { Token = token });
                        break;
                    case "global":
                        stack.Peek().Nodes.Add(new GlobalNode { Token = token});
                        break;
                    case "foreach":
                        stack.Push(new IterationNode { Nodes = new List<TextNode>(), Token = token });
                        break;
                    case "endforeach":
                        if (stack.Count() <= 1)
                            return Resources.IterationForToken0IsNotOpened.Formato(token);
                        var n = stack.Pop();
                        if (match.Groups["token"].Value != n.Token)
                            return Resources.IterationForToken0IsNotOpened.Formato(token);
                        stack.Peek().Nodes.Add(n);
                        break;
                    default:
                        break;
                }
                index = match.Index + match.Length;
            }
            if (stack.Count != 1)
                return Resources.IterationForToken0IsNotClosed.Formato(stack.Peek().Token);
            var lastM = matches.Cast<Match>().LastOrDefault();
            if (lastM != null && lastM.Index + lastM.Length < text.Length)
                stack.Peek().Nodes.Add(new LiteralNode { Text = text.Substring(lastM.Index + lastM.Length) });
            stack.Pop();
            return null;
        }


        #endregion

        public static void SendMail(this IIdentifiable entity, Enum systemTemplate)
        {
            var email = CreateEmailMessage(entity, EnumLogic<SystemTemplateDN>.ToEntity(systemTemplate));
            SenderManager.Send(email);
        }

        public static void SendMail(this IIdentifiable entity, EmailTemplateDN template)
        {
            var email = CreateEmailMessage(entity, template);
            SenderManager.Send(email);
        }

        public static void SendMailAsync(this IIdentifiable entity, Enum systemTemplate)
        {
            var email = CreateEmailMessage(entity, EnumLogic<SystemTemplateDN>.ToEntity(systemTemplate));
            SenderManager.SendAsync(email);
        }

        public static void SendMailAsync(this IIdentifiable entity, EmailTemplateDN template)
        {
            var email = CreateEmailMessage(entity, template);
            SenderManager.SendAsync(email);
        }


        public static void SafeSendMailAsync(this SmtpClient client, MailMessage message, Action<AsyncCompletedEventArgs> onComplete)
        {
            client.SendCompleted += (object sender, AsyncCompletedEventArgs e) =>
            {
                //client.Dispose(); -> the client can be used later by other messages
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
    }

    public class TemplateCultures
    {
        public TemplateCultures(string defaultCulture, params string[] otherCultures)
        {
            Default = CultureInfo.GetCultureInfo(defaultCulture);
            OtherCultures = new List<CultureInfo>();
            if (otherCultures != null)
                OtherCultures.AddRange(otherCultures.Select(s => CultureInfo.GetCultureInfo(s)));
        }

        public CultureInfo Default;
        public List<CultureInfo> OtherCultures;

        public MList<EmailTemplateMessageDN> CreateMessages(Func<EmailTemplateMessageDN> func)
        {
            var list = new MList<EmailTemplateMessageDN>();
            using (Sync.ChangeBothCultures(Default))
            {
                list.Add(func());
            }
            foreach (var ci in OtherCultures)
            {
                using (Sync.ChangeBothCultures(ci))
                {
                    list.Add(func());
                }
            }
            return list;
        }
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

    public class GlobalDispatcher
    {
        public IIdentifiable Entity;
        public CultureInfo Culture;
        public bool IsHtml;
    }

    public class EmailSenderManager
    {
        public EmailSenderManager(string urlPrefix, string defaultFrom, string defaultDisplayFrom, string defaultBcc)
        {
            GlobalTokens.Add("UrlPrefix", _ => urlPrefix);
            DefaultFrom = defaultFrom;
            DefaultDisplayFrom = defaultDisplayFrom;
            DefaultBcc = defaultBcc;
        }

        public string DefaultFrom;
        public string DefaultDisplayFrom;
        public string DefaultBcc;

        public Dictionary<string, Func<GlobalDispatcher, string>> GlobalTokens = new Dictionary<string, Func<GlobalDispatcher, string>>();

        protected MailMessage CreateMailMessage(EmailMessageDN email)
        {
            email.To = GetRecipient(email, null);

            MailMessage message = new MailMessage()
            {
                To = { new MailAddress(email.To) },
                From = email.DisplayFrom.HasText() ? new MailAddress(email.From, email.DisplayFrom) : new MailAddress(email.From),
                Subject = email.Subject,
                Body = email.Text,
                IsBodyHtml = email.IsBodyHtml,
            };

            if (email.Bcc.HasText())
                message.Bcc.AddRange(email.Bcc.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(a => new MailAddress(a)).ToList());
            if (email.Cc.HasText())
                message.CC.AddRange(email.Cc.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(a => new MailAddress(a)).ToList());
            return message;
        }

        protected virtual string GetRecipient(EmailMessageDN email, object[] args)
        {
            return email.Recipient.InDB().Select(r => r.Email).SingleEx();
        }

        public virtual void Send(EmailMessageDN email)
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
                if (Transaction.AvoidIndependentTransactions) //Transaction.IsTestTransaction
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

        public Lite<SMTPConfigurationDN> DefaultSMTPConfiguration;

        SmtpClient CreateSmtpClient(EmailMessageDN email)
        {
            return email.Template != null
                ? email.Template.InDB().Select(t => t.SMTPConfiguration).SingleOrDefault().TryCC(c => c.GenerateSmtpClient(true))
                : null
                ?? DefaultSMTPConfiguration.TryCC(c => c.GenerateSmtpClient(true))
                ?? EmailLogic.SafeSmtpClient();
        }

        public virtual void SendAsync(EmailMessageDN email)
        {
            try
            {
                SmtpClient client = CreateSmtpClient(email);

                MailMessage message = CreateMailMessage(email);

                email.Sent = null;
                email.Received = null;
                email.Save();

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

                    for (int i = 0; i < 4; i++) //to allow main thread to save email
                    {
                        if (email.InDB().UnsafeUpdate(updater) > 0)
                            return;

                        if (i != 3)
                            Thread.Sleep(3000);
                    }
                });
            }
            catch (Exception ex)
            {
                if (Transaction.AvoidIndependentTransactions) //Transaction.InTestTransaction
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

    }


}
