using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Mailing;
using Signum.Engine.Operations;
using Signum.Utilities;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Entities.UserQueries;
using System.Net.Configuration;
using System.Globalization;
using System.Configuration;
using Signum.Engine.UserQueries;
using System.Linq.Expressions;
using Signum.Entities.Translation;
using Signum.Engine.Translation;
using System.Text.RegularExpressions;

namespace Signum.Engine.Mailing
{
    public static class EmailTemplateLogic
    {   
        public static EmailTemplateMessageDN GetCultureMessage(this EmailTemplateDN template, CultureInfo ci)
        {
            return template.Messages.SingleOrDefault(tm => tm.CultureInfo.ToCultureInfo() == ci);
        }
     
        static Expression<Func<SystemEmailDN, IQueryable<EmailTemplateDN>>> EmailTemplatesExpression =
            se => Database.Query<EmailTemplateDN>().Where(et => et.SystemEmail == se);
        public static IQueryable<EmailTemplateDN> EmailTemplates(this SystemEmailDN se)
        {
            return EmailTemplatesExpression.Evaluate(se);
        }
        
        public static ResetLazy<Dictionary<Lite<EmailTemplateDN>, EmailTemplateDN>> EmailTemplatesLazy; 

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<EmailTemplateDN>();       

                EmailTemplatesLazy = sb.GlobalLazy(() => Database.Query<EmailTemplateDN>()
                    .Where(et => et.Active && (et.EndDate == null || et.EndDate > TimeZoneManager.Now))
                    .ToDictionary(et => et.ToLite()), new InvalidateWith(typeof(EmailTemplateDN)));

                SystemEmailLogic.Start(sb, dqm);
                EmailMasterTemplateLogic.Start(sb, dqm);

                dqm.RegisterQuery(typeof(EmailTemplateDN), () =>
                    from t in Database.Query<EmailTemplateDN>()
                    select new
                    {
                        Entity = t,
                        t.Id,
                        t.Name,
                        Active = t.IsActiveNow(),
                        t.IsBodyHtml
                    });

                sb.Schema.EntityEvents<EmailTemplateDN>().PreSaving += new PreSavingEventHandler<EmailTemplateDN>(EmailTemplate_PreSaving);
                sb.Schema.EntityEvents<EmailTemplateDN>().Retrieved += EmailTemplateLogic_Retrieved;

                Validator.OverridePropertyValidator((EmailTemplateMessageDN m) => m.Text).StaticPropertyValidation +=
                    EmailTemplateMessageText_StaticPropertyValidation;

                Validator.OverridePropertyValidator((EmailTemplateMessageDN m) => m.Subject).StaticPropertyValidation +=
                    EmailTemplateMessageSubject_StaticPropertyValidation;

                EmailTemplateGraph.Register();

                EmailTemplateParser.GlobalVariables.Add("UrlLeft", _ => EmailLogic.Configuration.UrlLeft);

                sb.Schema.Synchronizing += Schema_Synchronize_Tokens;
                sb.Schema.Synchronizing += Schema_Syncronize_DefaultTemplates;

                Validator.PropertyValidator<EmailTemplateDN>(et => et.Messages).StaticPropertyValidation += (et, pi) =>
                {
                    if (et.Active && !et.Messages.Any(m => m.CultureInfo.Is(EmailLogic.Configuration.DefaultCulture)))
                        return EmailTemplateMessage.ThereMustBeAMessageFor0.NiceToString().Formato(EmailLogic.Configuration.DefaultCulture.EnglishName);

                    return null;
                }; 
            }
        }

        static void EmailTemplateLogic_Retrieved(EmailTemplateDN emailTemplate)
        {
            using (emailTemplate.DisableAuthorization ? ExecutionMode.Global() : null)
            {
                object queryName = QueryLogic.ToQueryName(emailTemplate.Query.Key);
                QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);

                using (emailTemplate.DisableAuthorization ? ExecutionMode.Global() : null)
                    emailTemplate.ParseData(description);
            }
        }

        static string EmailTemplateMessageText_StaticPropertyValidation(EmailTemplateMessageDN message, PropertyInfo pi)
        {
            EmailTemplateParser.BlockNode parsedNode = message.TextParsedNode as EmailTemplateParser.BlockNode;

            if (parsedNode == null)
            {
                try
                {
                    parsedNode = ParseTemplate(message.Template, message.Text);
                    message.TextParsedNode = parsedNode;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }

            return null;
        }

        static string EmailTemplateMessageSubject_StaticPropertyValidation(EmailTemplateMessageDN message, PropertyInfo pi)
        {
            EmailTemplateParser.BlockNode parsedNode = message.SubjectParsedNode as EmailTemplateParser.BlockNode;

            if (parsedNode == null)
            {
                try
                {
                    parsedNode = ParseTemplate(message.template, message.Subject);
                    message.SubjectParsedNode = parsedNode;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }

            return null;
        }

        private static EmailTemplateParser.BlockNode ParseTemplate(EmailTemplateDN template, string text)
        {
            using (template.DisableAuthorization ? ExecutionMode.Global() : null)
            {
                object queryName = QueryLogic.ToQueryName(template.Query.Key);
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

                List<QueryToken> list = new List<QueryToken>();
                return EmailTemplateParser.Parse(text, qd, template.SystemEmail.ToType());
            }
        }

        static void EmailTemplate_PreSaving(EmailTemplateDN template, ref bool graphModified)
        {
            graphModified |= UpdateTokens(template);
        }

        public static bool UpdateTokens(EmailTemplateDN template)
        {
            using (template.DisableAuthorization ? ExecutionMode.Global() : null)
            {
                var queryName = QueryLogic.ToQueryName(template.Query.Key);
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

                List<QueryToken> list = new List<QueryToken>();

                if (template.From != null)
                    list.Add(template.From.Token.Token);

                foreach (var tr in template.Recipients.Where(r => r.Token != null))
                {
                    list.Add(tr.Token.Token);
                }

                foreach (var message in template.Messages)
                {
                    EmailTemplateParser.Parse(message.Text, qd, template.SystemEmail.ToType()).FillQueryTokens(list);
                    EmailTemplateParser.Parse(message.Subject, qd, template.SystemEmail.ToType()).FillQueryTokens(list);
                }

                var tokens = list.Distinct();

                if (template.Tokens.Any(t => t.ParseException != null) || !template.Tokens.Select(a => a.Token).ToList().SequenceEqual(tokens))
                {
                    template.Tokens.ResetRange(tokens.Select(t => new QueryTokenDN(t)));
                    return true;
                }
                return false;
            }
        }

        public static IEnumerable<EmailMessageDN> CreateEmailMessage(this Lite<EmailTemplateDN> liteTemplate, IIdentifiable entity, ISystemEmail systemEmail = null)
        {
            EmailTemplateDN template = EmailTemplatesLazy.Value.GetOrThrow(liteTemplate, "Email template {0} not in cache".Formato(liteTemplate));

            using (template.DisableAuthorization ? ExecutionMode.Global() : null)
                return new EmailMessageBuilder(template, entity, systemEmail).CreateEmailMessageInternal().ToList();
        }

        class EmailTemplateGraph : Graph<EmailTemplateDN>
        {
            static bool registered;
            public static bool Registered { get { return registered; } }

            public static void Register()
            {
                new Construct(EmailTemplateOperation.Create)
                {
                    Construct = _ => new EmailTemplateDN 
                    { 
                        SmtpConfiguration = SmtpConfigurationLogic.DefaultSmtpConfiguration.Value.ToLite(),
                        MasterTemplate = EmailMasterTemplateLogic.GetDefaultMasterTemplate(),
                    }
                }.Register();

                new Execute(EmailTemplateOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (t, _) => { }
                }.Register();

                new Execute(EmailTemplateOperation.Enable) 
                {
                    CanExecute = t => t.Active ? EmailTemplateMessage.TheTemplateIsAlreadyActive.NiceToString() : null,
                    Execute = (t, _) => t.Active = true
                }.Register();

                new Execute(EmailTemplateOperation.Disable) 
                {
                    CanExecute = t => !t.Active ? EmailTemplateMessage.TheTemplateIsAlreadyInactive.NiceToString() : null,
                    Execute = (t, _) => t.Active = false
                }.Register();

                registered = true;
            }
        }

        public static SqlPreCommand Schema_Synchronize_Tokens(Replacements replacements)
        {
            StringDistance sd = new StringDistance();

            var emailTemplates = Database.Query<EmailTemplateDN>().ToList();

            var table = Schema.Current.Table(typeof(EmailTemplateDN));

            SqlPreCommand cmd = emailTemplates.Select(uq => ProcessEmailTemplate(replacements, table, uq, sd)).Combine(Spacing.Double);

            return cmd;
        }

        static SqlPreCommand Schema_Syncronize_DefaultTemplates(Replacements replacements)
        {
            var table = Schema.Current.Table(typeof(EmailTemplateDN));

            var systemEmails = Database.Query<SystemEmailDN>().Where(se => !se.EmailTemplates().Any(a => a.Active)).ToList();

            string cis = Database.Query<CultureInfoDN>().Select(a => a.Name).ToString(", ").Etc(60);

            if (!systemEmails.Any() || !SafeConsole.Ask("{0}\r\n have no EmailTemplates. Create in {1}?".Formato(systemEmails.ToString("\r\n"), cis.DefaultText("No CultureInfos registered!"))))
                return null;

            var cmd = systemEmails
                    .Select(se =>
                    {
                        try
                        {
                            return table.InsertSqlSync(SystemEmailLogic.CreateDefaultTemplate(se), includeCollections: true);
                        }
                        catch (Exception e)
                        {
                            return new SqlPreCommandSimple("Exception on SystemEmail {0}: {1}".Formato(se, e.Message));
                        }
                    })
                    .Combine(Spacing.Double);

            if (cmd != null)
                return SqlPreCommand.Combine(Spacing.Double, new SqlPreCommandSimple("DECLARE @idParent INT"), cmd);

            return cmd;
        }

        static SqlPreCommand ProcessEmailTemplate(Replacements replacements, Table table, EmailTemplateDN et, StringDistance sd)
        {
            try
            {
                Console.Clear();

                SafeConsole.WriteLineColor(ConsoleColor.White, "EmailTemplate: " + et.Name);
                Console.WriteLine(" Query: " + et.Query.Key);


                var result = (from msg in et.Messages
                              from s in new[] { msg.Subject, msg.Text }
                              from m in EmailTemplateParser.KeywordsRegex.Matches(s).Cast<Match>()
                              where m.Groups["token"].Success && !IsToken(m.Groups["keyword"].Value)
                              select new
                              {
                                  isGlobal = IsGlobal(m.Groups["keyword"].Value),
                                  token = m.Groups["token"].Value
                              }).Distinct().ToList();

                foreach (var g in result.Where(a => a.isGlobal).Select(a => a.token).ToHashSet())
                {
                    if(!EmailTemplateParser.GlobalVariables.ContainsKey(g))
                    {
                        string s = replacements.SelectInteractive(g, EmailTemplateParser.GlobalVariables.Keys, "EmailTemplate Globals", sd);

                        if (s != null)
                        {
                            EmailTemplateParser.ReplaceToken(et, (keyword, oldToken) =>
                            {
                                if (!IsGlobal(keyword))
                                    return null;

                                if (oldToken == g)
                                    return s;

                                return null;
                            });
                        }
                    }
                }

                foreach (var m in result.Where(a => !a.isGlobal).Select(a => a.token).ToHashSet())
                {
                    var type = et.SystemEmail.ToType();

                    string newM = GetNewModel(type, m, replacements, sd);

                    if (newM != m)
                    {
                        EmailTemplateParser.ReplaceToken(et, (keyword, oldToken) =>
                        {
                            if (!IsModel(keyword))
                                return null;

                            if (oldToken == m)
                                return newM;

                            return null;
                        });
                    }
                }

                if (et.Tokens.Any(a => a.ParseException != null))
                    using (et.DisableAuthorization ? ExecutionMode.Global() : null)
                    {
                        QueryDescription qd = DynamicQueryManager.Current.QueryDescription(et.Query.ToQueryName());

                        if (et.Tokens.Any())
                        {
                            Console.WriteLine(" Tokens:");
                            foreach (var item in et.Tokens.ToList())
                            {
                                QueryTokenDN token = item;
                                switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, false, "", allowRemoveToken: false))
                                {
                                    case FixTokenResult.Nothing: break;
                                    case FixTokenResult.DeleteEntity: return table.DeleteSqlSync(et);
                                    case FixTokenResult.RemoveToken: throw new InvalidOperationException("Unexpected RemoveToken");
                                    case FixTokenResult.SkipEntity: return null;
                                    case FixTokenResult.Fix:
                                        foreach (var tok in et.Recipients.Where(r => r.Token.Equals(item)).ToList())
                                            tok.Token = token;
                                        
                                        EmailTemplateParser.ReplaceToken(et, (keyword, oldToken)=>
                                        {
                                            if(!IsToken(keyword))
                                                return null;

                                            if(AreSimilar(oldToken, item.TokenString))
                                                return token.TokenString;

                                            return null;
                                        });
                                        break;
                                    default: break;
                                }
                            }
                        }
                    }

                Console.Clear();

                return table.UpdateSqlSync(et, includeCollections: true);
            }
            catch (Exception e)
            {
                return new SqlPreCommandSimple("-- Exception in {0}: {1}".Formato(et.BaseToString(), e.Message));
            }
        }

        static string GetNewModel(Type type, string model, Replacements replacements, StringDistance sd)
        {
            List<string> fields = new List<string>();

            foreach (var field in model.Split('.'))
            {
                var info = (MemberInfo)type.GetField(field, EmailTemplateParser.ModelNode.flags) ??
                         (MemberInfo)type.GetProperty(field, EmailTemplateParser.ModelNode.flags);

                if (info != null)
                {
                    type = info.ReturningType();
                    fields.Add(info.Name);
                }
                else
                {
                    var allMembers = type.GetFields(EmailTemplateParser.ModelNode.flags).Select(a => a.Name)
                        .Concat(type.GetProperties(EmailTemplateParser.ModelNode.flags).Select(a => a.Name));

                    string s = replacements.SelectInteractive(field, allMembers, "Members {0}".Formato(type.FullName), sd);

                    if (s == null)
                        return null;

                    fields.Add(s);
                }
            }

            return fields.ToString(".");
        }

        private static bool AreSimilar(string p1, string p2)
        {
            if (p1.StartsWith("Entity."))
                p1 = p1.After("Entity.");

            if (p2.StartsWith("Entity."))
                p2 = p2.After("Entity.");

            return p1 == p2;
        }

        public static bool IsModel(string keyword)
        {
            return keyword == "model" || keyword == "modelraw";
        }

        public static bool IsGlobal(string keyword)
        {
            return keyword == "global";
        }

        public static bool IsToken(string keyword)
        {
            return
                keyword == "" ||
                keyword == "foreach" ||
                keyword == "if" ||
                keyword == "raw" ||
                keyword == "any";
        }

        static KeywordType GetTokenType(string keyword)
        {
            switch (keyword)
            {
                case "foreach":
                case "if":
                case "raw":
                case "any": return KeywordType.Token;
                case "model":
                case "modelraw": return KeywordType.Model;

                case "global": return KeywordType.Global;
            }

            throw new InvalidOperationException("Unexpected keyword '{0}'".Formato(keyword));
        }

        enum KeywordType
        {
            Token,
            Model,
            Global,
        }
    }
}
