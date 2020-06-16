using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Signum.Engine.Basics;
using Signum.Engine.Templating;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Mailing;
using Signum.Utilities;

namespace Signum.Engine.Mailing
{
    class EmailMessageBuilder
    {
        EmailTemplateEntity template;
        Entity? entity;
        IEmailModel? model;
        object queryName;
        QueryDescription qd;
        EmailSenderConfigurationEntity? smtpConfig;

        public EmailMessageBuilder(EmailTemplateEntity template, Entity? entity, IEmailModel? systemEmail)
        {
            this.template = template;
            this.entity = entity;
            this.model = systemEmail;

            this.queryName = QueryLogic.ToQueryName(template.Query.Key);
            this.qd = QueryLogic.Queries.QueryDescription(queryName);
            this.smtpConfig = EmailTemplateLogic.GetSmtpConfiguration?.Invoke(template, (systemEmail?.UntypedEntity as Entity ?? entity)?.ToLite());
        }

        ResultTable table = null!;
        Dictionary<QueryToken, ResultColumn> dicTokenColumn = null!;
        IEnumerable<ResultRow> currentRows = null!;


        public IEnumerable<EmailMessageEntity> CreateEmailMessageInternal()
        {
            ExecuteQuery();

            foreach (EmailAddressEmbedded from in GetFrom())
            {
                foreach (List<EmailOwnerRecipientData> recipients in GetRecipients())
                {
                    EmailMessageEntity email;
                    try
                    {
                        CultureInfo ci = recipients.Where(a => a.Kind == EmailRecipientKind.To).Select(a => a.OwnerData.CultureInfo).FirstOrDefault()?.ToCultureInfo() ??
                            EmailLogic.Configuration.DefaultCulture.ToCultureInfo();

                        email = new EmailMessageEntity
                        {
                            Target = entity?.ToLite() ?? (this.model!.UntypedEntity as Entity)?.ToLite(),
                            Recipients = recipients.Select(r => new EmailRecipientEmbedded(r.OwnerData) { Kind = r.Kind }).ToMList(),
                            From = from,
                            IsBodyHtml = template.IsBodyHtml,
                            EditableMessage = template.EditableMessage,
                            Template = template.ToLite(),
                            Attachments = template.Attachments.SelectMany(g => EmailTemplateLogic.GenerateAttachment.Invoke(g,
                            new EmailTemplateLogic.GenerateAttachmentContext(this.qd, template, dicTokenColumn, currentRows, ci)
                            {
                                ModelType = template.Model?.ToType(),
                                Model = model,
                                Entity = entity,
                            })).ToMList()
                        };

                        EmailTemplateMessageEmbedded message = template.GetCultureMessage(ci) ?? template.GetCultureMessage(EmailLogic.Configuration.DefaultCulture.ToCultureInfo());

                        if (message == null)
                            throw new InvalidOperationException("Message {0} does not have a message for CultureInfo {1} (or Default)".FormatWith(template, ci));

                        using (CultureInfoUtils.ChangeBothCultures(ci))
                        {
                            email.Subject = SubjectNode(message).Print(
                                new TextTemplateParameters(entity, ci, dicTokenColumn, currentRows)
                                {
                                    IsHtml = false,
                                    Model = model
                                });

                            email.Body = TextNode(message).Print(
                                new TextTemplateParameters(entity, ci, dicTokenColumn, currentRows)
                                {
                                    IsHtml = template.IsBodyHtml,
                                    Model = model,
                                });
                        }

                    }
                    catch (Exception ex)
                    {
                        ex.Data["Template"] = this.template.ToLite();
                        ex.Data["Model"] = this.model;
                        ex.Data["Entity"] = this.entity;
                        throw;
                    }


                    yield return email;
                }
            }
        }
            
        TextTemplateParser.BlockNode TextNode(EmailTemplateMessageEmbedded message)
        {
            if (message.TextParsedNode == null)
            {
                string body = message.Text;

                if (template.MasterTemplate != null)
                {
                    var emt = template.MasterTemplate.RetrieveAndRemember();
                    var emtm = emt.GetCultureMessage(message.CultureInfo.ToCultureInfo()) ??
                        emt.GetCultureMessage(EmailLogic.Configuration.DefaultCulture.ToCultureInfo());

                    if (emtm != null)
                        body = EmailMasterTemplateEntity.MasterTemplateContentRegex.Replace(emtm.Text, m => body);
                }

                message.TextParsedNode = TextTemplateParser.Parse(body, qd, template.Model?.ToType());
            }

            return (TextTemplateParser.BlockNode)message.TextParsedNode;
        }

        TextTemplateParser.BlockNode SubjectNode(EmailTemplateMessageEmbedded message)
        {
            if (message.SubjectParsedNode == null)
                message.SubjectParsedNode = TextTemplateParser.Parse(message.Subject, qd, template.Model?.ToType());

            return (TextTemplateParser.BlockNode)message.SubjectParsedNode;
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
                        yield return new EmailAddressEmbedded(currentRows.Select(r => (EmailOwnerData)r[owner]!).Distinct().SingleEx());
                    }
                    else
                    {
                        var groups = currentRows.GroupBy(r => (EmailOwnerData)r[owner]!);

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
                        Email = template.From.EmailAddress!,
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
                    throw new InvalidOperationException("Not Default From found");
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
                    Email = tr.EmailAddress!,
                    DisplayName = tr.DisplayName
                }) { Kind = tr.Kind }));

                if (model != null)
                    recipients.AddRange(model.GetRecipients());

                if (smtpConfig != null)
                {
                    recipients.AddRange(smtpConfig.AdditionalRecipients.Where(a => a.EmailOwner == null).Select(r =>
                        new EmailOwnerRecipientData(new EmailOwnerData { CultureInfo = null, DisplayName = r.DisplayName, Email = r.EmailAddress, Owner = r.EmailOwner }) { Kind = r.Kind }));
                }

                if (recipients.Where(r=>r.OwnerData.Email.HasText()).Any())
                    yield return recipients;
            }
        }

        private IEnumerable<List<EmailOwnerRecipientData>> TokenRecipients(List<EmailTemplateRecipientEmbedded> tokenRecipients)
        {
            if (!template.SendDifferentMessages)
            {
                return new[]
                { 
                    tokenRecipients.SelectMany(tr =>
                    {
                        ResultColumn owner = dicTokenColumn.GetOrThrow(tr.Token!.Token);

                        List<EmailOwnerData> groups = currentRows.Select(r => (EmailOwnerData)r[owner]!).Distinct().ToList();

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

        private IEnumerable<List<EmailOwnerRecipientData>> CrossProduct(List<EmailTemplateRecipientEmbedded> tokenRecipients, int pos)
        {
            if (tokenRecipients.Count == pos)
                yield return new List<EmailOwnerRecipientData>();
            else
            {
                EmailTemplateRecipientEmbedded tr = tokenRecipients[pos];

                ResultColumn owner = dicTokenColumn.GetOrThrow(tr.Token!.Token);

                var groups = currentRows.GroupBy(r => (EmailOwnerData)r[owner]!).ToList();

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
            using (this.template.DisableAuthorization ? ExecutionMode.Global() : null)
            {
                List<QueryToken> tokens = new List<QueryToken>();
                if (template.From != null && template.From.Token != null)
                    tokens.Add(template.From.Token.Token);

                foreach (var tr in template.Recipients.Where(r => r.Token != null))
                    tokens.Add(tr.Token!.Token);

                foreach (var t in template.Messages)
                {
                    TextNode(t).FillQueryTokens(tokens);
                    SubjectNode(t).FillQueryTokens(tokens);
                }

                foreach (var a in template.Attachments)
                {
                    EmailTemplateLogic.FillAttachmentTokens.Invoke(a, new EmailTemplateLogic.FillAttachmentTokenContext(qd, tokens)
                    {
                        ModelType = template.Model?.ToType(),
                    });
                }

                var columns = tokens.Distinct().Select(qt => new Column(qt, null)).ToList();

                var filters = model != null ? model.GetFilters(qd) :
                    new List<Filter> { new FilterCondition(QueryUtils.Parse("Entity", qd, 0), FilterOperation.EqualTo, entity!.ToLite()) };

                this.table = QueryLogic.Queries.ExecuteQuery(new QueryRequest
                {
                    QueryName = queryName,
                    Columns = columns,
                    Pagination = model?.GetPagination() ?? new Pagination.All(),
                    Filters = filters,
                    Orders = model?.GetOrders(qd) ?? new List<Order>(),
                });

                this.dicTokenColumn = table.Columns.ToDictionary(rc => rc.Column.Token);

                this.currentRows = table.Rows;
            }
        }
    }
}
