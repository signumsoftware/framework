using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Mailing;
using Signum.Utilities;
using Signum.Entities.Translation;
using Signum.Engine.Translation;
using Signum.Engine.Mailing;
using System.Threading;

namespace Signum.Engine.Mailing
{
    class EmailMessageBuilder
    {
        EmailTemplateEntity template;
        Entity entity;
        ISystemEmail systemEmail;
        object queryName;
        QueryDescription qd;
        SmtpConfigurationEntity smtpConfig;

        public EmailMessageBuilder(EmailTemplateEntity template, Entity entity, ISystemEmail systemEmail)
        {
            this.template = template;
            this.entity = entity;
            this.systemEmail = systemEmail;

            this.queryName = QueryLogic.ToQueryName(template.Query.Key);
            this.qd = DynamicQueryManager.Current.QueryDescription(queryName);
            this.smtpConfig = EmailTemplateLogic.GetSmtpConfiguration == null ? null : EmailTemplateLogic.GetSmtpConfiguration(template);
        }

        ResultTable table;
        Dictionary<QueryToken, ResultColumn> dicTokenColumn;
        IEnumerable<ResultRow> currentRows;


        public IEnumerable<EmailMessageEntity> CreateEmailMessageInternal()
        {
            ExecuteQuery();

            foreach (EmailAddressEmbedded from in GetFrom())
            {
                foreach (List<EmailOwnerRecipientData> recipients in GetRecipients())
                {
                    CultureInfo ci = recipients.Where(a => a.Kind == EmailRecipientKind.To).Select(a => a.OwnerData.CultureInfo).FirstOrDefault().ToCultureInfo() ?? EmailLogic.Configuration.DefaultCulture.ToCultureInfo();

                    EmailMessageEntity email = new EmailMessageEntity
                    {
                        Target = entity?.ToLite() ?? (this.systemEmail.UntypedEntity as Entity)?.ToLite(),
                        Recipients = recipients.Select(r => new EmailRecipientEntity(r.OwnerData) { Kind = r.Kind }).ToMList(),
                        From = from,
                        IsBodyHtml = template.IsBodyHtml,
                        EditableMessage = template.EditableMessage,
                        Template = template.ToLite(),
                        Attachments = template.Attachments.SelectMany(g => EmailTemplateLogic.GenerateAttachment.Invoke(g, new EmailTemplateLogic.GenerateAttachmentContext
                        {
                            QueryDescription = this.qd,
                            ModelType = template.SystemEmail.ToType(),
                            SystemEmail = systemEmail,
                            CurrentRows = currentRows,
                            ResultColumns = dicTokenColumn,
                            Entity = entity, 
                            Template = template,
                            Culture = ci,
                        })).ToMList()
                    };
                    
                    EmailTemplateMessageEmbedded message = template.GetCultureMessage(ci) ?? template.GetCultureMessage(EmailLogic.Configuration.DefaultCulture.ToCultureInfo());

                    if (message == null)
                        throw new InvalidOperationException("Message {0} does not have a message for CultureInfo {1} (or Default)".FormatWith(template, ci));

                    using (CultureInfoUtils.ChangeBothCultures(ci))
                    {
                        email.Subject = SubjectNode(message).Print(
                            new EmailTemplateParameters(entity, ci, dicTokenColumn, currentRows)
                            {
                                IsHtml = false,
                                SystemEmail = systemEmail
                            });

                        email.Body = TextNode(message).Print(
                            new EmailTemplateParameters(entity, ci, dicTokenColumn, currentRows)
                            {
                                IsHtml = template.IsBodyHtml,
                                SystemEmail = systemEmail,
                            });
                    }


                    yield return email;
                }
            }
        }

        EmailTemplateParser.BlockNode TextNode(EmailTemplateMessageEmbedded message)
        {
            if (message.TextParsedNode == null)
            {
                string body = message.Text;

                if (template.MasterTemplate != null)
                {
                    var emt = template.MasterTemplate.Retrieve();
                    var emtm = emt.GetCultureMessage(message.CultureInfo.ToCultureInfo()) ??
                        emt.GetCultureMessage(EmailLogic.Configuration.DefaultCulture.ToCultureInfo());

                    if (emtm != null)
                        body = EmailMasterTemplateEntity.MasterTemplateContentRegex.Replace(emtm.Text, m => body);
                }

                message.TextParsedNode = EmailTemplateParser.Parse(body, qd, template.SystemEmail.ToType());
            }

            return (EmailTemplateParser.BlockNode)message.TextParsedNode;
        }

        EmailTemplateParser.BlockNode SubjectNode(EmailTemplateMessageEmbedded message)
        {
            if (message.SubjectParsedNode == null)
                message.SubjectParsedNode = EmailTemplateParser.Parse(message.Subject, qd, template.SystemEmail.ToType());

            return (EmailTemplateParser.BlockNode)message.SubjectParsedNode;
        }

        IEnumerable<EmailAddressEmbedded> GetFrom()
        {
            if (template.From != null)
            {
                if (template.From.Token != null)
                {
                    ResultColumn owner = dicTokenColumn.GetOrThrow(template.From.Token.Token);

                    if (!template.SendDifferentMessages)
                    {
                        yield return new EmailAddressEmbedded(currentRows.Select(r => (EmailOwnerData)r[owner]).Distinct().SingleEx());
                    }
                    else
                    {
                        var groups = currentRows.GroupBy(r => (EmailOwnerData)r[owner]);

                        if (groups.Count() == 1 && groups.Single().Key?.Owner == null)
                            yield break;
                        else
                        {
                            foreach (var gr in groups)
                            {
                                var old = currentRows;
                                currentRows = gr;

                                yield return new EmailAddressEmbedded(gr.Key);

                                currentRows = old;
                            }
                        }
                    }
                }
                else
                {
                    yield return new EmailAddressEmbedded(new EmailOwnerData
                    {
                        CultureInfo = null,
                        Email = template.From.EmailAddress,
                        DisplayName = template.From.DisplayName,
                    });
                }
            }
            else
            {
                if (smtpConfig != null && smtpConfig.DefaultFrom != null)
                {
                    yield return smtpConfig.DefaultFrom.Clone();
                }
                else
                {
                    SmtpSection smtpSection = ConfigurationManager.GetSection("system.net/mailSettings/smtp") as SmtpSection;

                    yield return new EmailAddressEmbedded
                    {
                        EmailAddress = smtpSection.From
                    };
                }
            }
        }

        IEnumerable<List<EmailOwnerRecipientData>> GetRecipients()
        {
            foreach (List<EmailOwnerRecipientData> recipients in TokenRecipients(template.Recipients.Where(a => a.Token != null).ToList()))
            {
                recipients.AddRange(template.Recipients.Where(a => a.Token == null).Select(tr => new EmailOwnerRecipientData(new EmailOwnerData
                {
                    CultureInfo = null,
                    Email = tr.EmailAddress,
                    DisplayName = tr.DisplayName
                }) { Kind = tr.Kind }));

                if (systemEmail != null)
                    recipients.AddRange(systemEmail.GetRecipients());

                if (smtpConfig != null)
                {
                    recipients.AddRange(smtpConfig.AdditionalRecipients.Where(a => a.EmailOwner == null).Select(r =>
                        new EmailOwnerRecipientData(new EmailOwnerData { CultureInfo = null, DisplayName = r.DisplayName, Email = r.EmailAddress, Owner = r.EmailOwner }) { Kind = r.Kind }));
                }

                if (recipients.Where(r=>r.OwnerData.Email.HasText()).Any())
                    yield return recipients;
            }
        }

        private IEnumerable<List<EmailOwnerRecipientData>> TokenRecipients(List<EmailTemplateRecipientEntity> tokenRecipients)
        {
            if (!template.SendDifferentMessages)
            {
                return new[]
                { 
                    tokenRecipients.SelectMany(tr =>
                    {
                        ResultColumn owner = dicTokenColumn.GetOrThrow(tr.Token.Token);

                        List<EmailOwnerData> groups = currentRows.Select(r => (EmailOwnerData)r[owner]).Distinct().ToList();

                        if (groups.Count == 1 && groups[0]?.Email == null)
                            return new List<EmailOwnerRecipientData>();

                        return groups.Where(g => g.Email.HasText()).Select(g => new EmailOwnerRecipientData(g) { Kind = tr.Kind }).ToList();
                    }).ToList()
                };
            }
            else
            {
                return CrossProduct(tokenRecipients, 0);
            }
        }

        private IEnumerable<List<EmailOwnerRecipientData>> CrossProduct(List<EmailTemplateRecipientEntity> tokenRecipients, int pos)
        {
            if (tokenRecipients.Count == pos)
                yield return new List<EmailOwnerRecipientData>();
            else
            {
                EmailTemplateRecipientEntity tr = tokenRecipients[pos];

                ResultColumn owner = dicTokenColumn.GetOrThrow(tr.Token.Token);

                var groups = currentRows.GroupBy(r => (EmailOwnerData)r[owner]).ToList();

                if (groups.Count == 1 && groups[0].Key?.Email == null)
                {
                    yield return new List<EmailOwnerRecipientData>();
                }
                else
                {
                    foreach (var gr in groups)
                    {
                        var rec = new EmailOwnerRecipientData(gr.Key) { Kind = tr.Kind };

                        var old = currentRows;
                        currentRows = gr;

                        foreach (var list in CrossProduct(tokenRecipients, pos + 1))
                        {
                            var result = list.ToList();
                            result.Insert(0, rec);
                            yield return result;
                        }
                        currentRows = old;
                    }
                }
            }
        }

        void ExecuteQuery()
        {
            using (ExecutionMode.Global())
            {
                List<QueryToken> tokens = new List<QueryToken>();
                if (template.From != null && template.From.Token != null)
                    tokens.Add(template.From.Token.Token);

                foreach (var tr in template.Recipients.Where(r => r.Token != null))
                    tokens.Add(tr.Token.Token);

                foreach (var t in template.Messages)
                {
                    TextNode(t).FillQueryTokens(tokens);
                    SubjectNode(t).FillQueryTokens(tokens);
                }

                foreach (var a in template.Attachments)
                {
                    EmailTemplateLogic.FillAttachmentTokens.Invoke(a, new EmailTemplateLogic.FillAttachmentTokenContext
                    {
                        QueryDescription = qd, 
                        ModelType = template.SystemEmail.ToType(),
                        QueryTokens = tokens,
                    });
                }

                var columns = tokens.Distinct().Select(qt => new Column(qt, null)).ToList();

                var filters = systemEmail != null ? systemEmail.GetFilters(qd) :
                    new List<Filter> { new Filter(QueryUtils.Parse("Entity", qd, 0), FilterOperation.EqualTo, entity.ToLite()) };

                this.table = DynamicQueryManager.Current.ExecuteQuery(new QueryRequest
                {
                    QueryName = queryName,
                    Columns = columns,
                    Pagination = systemEmail?.GetPagination() ?? new Pagination.All(),
                    Filters = filters,
                    Orders = systemEmail?.GetOrders(qd) ?? new List<Order>(),
                });

                this.dicTokenColumn = table.Columns.ToDictionary(rc => rc.Column.Token);

                this.currentRows = table.Rows;
            }
        }
    }
}
