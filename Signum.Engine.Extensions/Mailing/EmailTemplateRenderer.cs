using System.Globalization;
using Signum.Engine.Templating;
using Signum.Entities.Basics;
using Signum.Entities.Mailing;
using Signum.Entities.UserQueries;

namespace Signum.Engine.Mailing;

class EmailMessageBuilder
{
    EmailTemplateEntity template;
    Entity? entity;
    IEmailModel? model;
    object queryName;
    QueryDescription qd;
    EmailSenderConfigurationEntity? smtpConfig;
    CultureInfo? cultureInfo;

    public EmailMessageBuilder(EmailTemplateEntity template, Entity? entity, IEmailModel? systemEmail, CultureInfo? cultureInfo)
    {
        this.template = template;
        this.entity = entity;
        this.model = systemEmail;

        this.queryName = QueryLogic.ToQueryName(template.Query.Key);
        this.qd = QueryLogic.Queries.QueryDescription(queryName);
        this.smtpConfig = EmailTemplateLogic.GetSmtpConfiguration?.Invoke(template, (systemEmail?.UntypedEntity as Entity)?.ToLiteFat(), null);
        this.cultureInfo = cultureInfo;
    }

    ResultTable table = null!;
    Dictionary<QueryToken, ResultColumn> dicTokenColumn = null!;
    IEnumerable<ResultRow> currentRows = null!;


    public IEnumerable<EmailMessageEntity> CreateEmailMessageInternal()
    {
        ExecuteQuery();

        foreach (EmailFromEmbedded from in GetFrom())
        {
            foreach (List<EmailOwnerRecipientData> recipients in GetRecipients())
            {
                EmailMessageEntity email = CreateEmailMessageInternal(from, recipients);

                yield return email;
            }
        }
    }

    private EmailMessageEntity CreateEmailMessageInternal(EmailFromEmbedded from, List<EmailOwnerRecipientData> recipients)
    {
        EmailMessageEntity email;
        try
        {
            var ci = this.cultureInfo ??
                 EmailTemplateLogic.GetCultureInfo?.Invoke(entity ?? model?.UntypedEntity as Entity) ??
                recipients.Where(a => a.Kind == EmailRecipientKind.To).Select(a => a.OwnerData.CultureInfo).FirstOrDefault()?.ToCultureInfo() ??
                EmailLogic.Configuration.DefaultCulture.ToCultureInfo();

            var context = new EmailTemplateLogic.GenerateAttachmentContext(this.qd, template, dicTokenColumn, currentRows, ci)
            {
                ModelType = template.Model?.ToType(),
                Model = model,
                Entity = entity,
            };

            email = new EmailMessageEntity
            {
                Target = entity?.ToLite() ?? (this.model!.UntypedEntity as Entity)?.ToLite(),
                Recipients = recipients.Select(r => new EmailRecipientEmbedded(r.OwnerData) { Kind = r.Kind }).ToMList(),
                From = from,
                IsBodyHtml = template.MessageFormat == EmailMessageFormat.HtmlComplex || template.MessageFormat == EmailMessageFormat.HtmlSimple,
                EditableMessage = template.EditableMessage,
                Template = template.ToLite(),
                Attachments = template.Attachments.Concat((template.MasterTemplate?.RetrieveAndRemember().Attachments).EmptyIfNull()).SelectMany(g => EmailTemplateLogic.GenerateAttachment.Invoke(g, context)).ToMList()
            };

            EmailTemplateMessageEmbedded? message = 
                template.GetCultureMessage(ci) ??
                template.GetCultureMessage(ci.Parent) ?? 
                template.GetCultureMessage(EmailLogic.Configuration.DefaultCulture.ToCultureInfo()) ??
                template.GetCultureMessage(EmailLogic.Configuration.DefaultCulture.ToCultureInfo().Parent);

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

                email.Body = new BigStringEmbedded(TextNode(message).Print(
                    new TextTemplateParameters(entity, ci, dicTokenColumn, currentRows)
                    {
                        IsHtml = template.MessageFormat == EmailMessageFormat.HtmlComplex || template.MessageFormat == EmailMessageFormat.HtmlSimple,
                        Model = model,
                    }));
            }

        }
        catch (Exception ex)
        {
            ex.Data["Template"] = this.template.ToLite();
            ex.Data["Model"] = this.model;
            ex.Data["Entity"] = this.entity;
            throw;
        }

        return email;
    }

    TextTemplateParser.BlockNode TextNode(EmailTemplateMessageEmbedded message)
    {
        if (message.TextParsedNode == null)
        {
            string body = message.Text;

            if (template.MasterTemplate != null)
            {
                var emt = template.MasterTemplate.RetrieveAndRemember();
                var emtm = 
                    emt.GetCultureMessage(message.CultureInfo.ToCultureInfo()) ??
                    emt.GetCultureMessage(message.CultureInfo.ToCultureInfo().Parent) ??
                    emt.GetCultureMessage(EmailLogic.Configuration.DefaultCulture.ToCultureInfo()) ??
                    emt.GetCultureMessage(EmailLogic.Configuration.DefaultCulture.ToCultureInfo().Parent);

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

    IEnumerable<EmailFromEmbedded> GetFrom()
    {
        if (template.From != null)
        {
            if (template.From.Token != null)
            {
                ResultColumn owner = dicTokenColumn.GetOrThrow(template.From.Token.Token);
                var groups = currentRows.GroupBy(r => (EmailOwnerData)r[owner]!).ToList();

                var groupsWithEmail = groups.Where(a => a.Key.Email.HasText()).ToList();

                if (groupsWithEmail.IsEmpty())
                {
                    switch (template.From.WhenNone)
                    {
                        case WhenNoneFromBehaviour.ThrowException:
                            if (groups.Count() == 0)
                                throw new InvalidOperationException($"Impossible to send {this.template} because From Token ({template.From.Token}) returned no result");
                            else
                                throw new InvalidOperationException($"Impossible to send {this.template} because From Token ({template.From.Token}) returned results without Email addresses");

                        case WhenNoneFromBehaviour.NoMessage:
                            yield break;
                        case WhenNoneFromBehaviour.DefaultFrom:
                            if (smtpConfig != null && smtpConfig.DefaultFrom != null)
                                yield return smtpConfig.DefaultFrom.Clone();
                            else
                                throw new InvalidOperationException("Not Default From found");
                            break;
                        default:
                            throw new UnexpectedValueException(template.From.WhenNone);
                    }
                }
                else
                {
                    if (template.From.WhenMany == WhenManyFromBehaviour.FistResult)
                        groupsWithEmail = groupsWithEmail.Take(1).ToList();

                    foreach (var gr in groupsWithEmail)
                    {
                        var old = currentRows;
                        currentRows = gr;

                        yield return new EmailFromEmbedded(gr.Key);

                        currentRows = old;
                    }
                }
            }
            else
            {
                yield return new EmailFromEmbedded
                {
                    EmailOwner = null,
                    EmailAddress = template.From.EmailAddress!,
                    DisplayName = template.From.DisplayName,
                    AzureUserId = template.From.AzureUserId,
                };
            }
        }
        else if(this.model?.GetFrom() is var from && from != null)
        {
            yield return new EmailFromEmbedded
            {
                EmailOwner = from.Owner,
                EmailAddress = from.Email!,
                DisplayName = from.DisplayName,
                AzureUserId = from.AzureUserId,
            };
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
        foreach (List<EmailOwnerRecipientData> recipients in TokenRecipientsCrossProduct(template.Recipients.Where(a => a.Token != null).ToList(), 0))
        {
            recipients.AddRange(template.Recipients.Where(a => a.Token == null).Select(tr => new EmailOwnerRecipientData(new EmailOwnerData
            {
                CultureInfo = null,
                Email = tr.EmailAddress!,
                DisplayName = tr.DisplayName
            })
            { Kind = tr.Kind }));

            if (model != null)
                recipients.AddRange(model.GetRecipients());

            if (smtpConfig != null)
            {
                recipients.AddRange(smtpConfig.AdditionalRecipients.Where(a => a.EmailOwner == null).Select(r =>
                    new EmailOwnerRecipientData(new EmailOwnerData { CultureInfo = null, DisplayName = r.DisplayName, Email = r.EmailAddress, Owner = r.EmailOwner }) { Kind = r.Kind }));
            }

            if (recipients.Where(r => r.OwnerData.Email.HasText()).Any())
                yield return recipients;
        }
    }


    private IEnumerable<List<EmailOwnerRecipientData>> TokenRecipientsCrossProduct(List<EmailTemplateRecipientEmbedded> tokenRecipients, int pos)
    {
        if (tokenRecipients.Count == pos)
            yield return new List<EmailOwnerRecipientData>();
        else
        {
            EmailTemplateRecipientEmbedded tr = tokenRecipients[pos];

            ResultColumn owner = dicTokenColumn.GetOrThrow(tr.Token!.Token);

            var groups = currentRows.GroupBy(r => (EmailOwnerData)r[owner]!).ToList();

            var groupsWithEmail = groups.Where(a => a.Key !=null && a.Key.Email.HasText()).ToList();
            
            if (groupsWithEmail.IsEmpty())
            {
                switch (tr.WhenNone)
                {
                    case WhenNoneRecipientsBehaviour.ThrowException:
                        throw new InvalidOperationException($"Impossible to send {this.template} because {tr.Kind} Token ({tr.Token}) returned no result or no result with Email address");

                    case WhenNoneRecipientsBehaviour.NoMessage:
                        yield break;
                    case WhenNoneRecipientsBehaviour.NoRecipients:
                        foreach (var item in TokenRecipientsCrossProduct(tokenRecipients, pos + 1))
                        {
                            yield return item;
                        }
                        break;
                    default:
                        throw new UnexpectedValueException(tr.WhenNone);
                }

            }
            else
            {
                if (tr.WhenMany == WhenManyRecipiensBehaviour.SplitMessages)
                {
                    foreach (var gr in groupsWithEmail)
                    {
                        var rec = new EmailOwnerRecipientData(gr.Key) { Kind = tr.Kind };

                        var old = currentRows;
                        currentRows = gr;

                        foreach (var list in TokenRecipientsCrossProduct(tokenRecipients, pos + 1))
                        {
                            var result = list.ToList();
                            result.Insert(0, rec);
                            yield return result;
                        }
                        currentRows = old;
                    }
                }
                else if (tr.WhenMany == WhenManyRecipiensBehaviour.KeepOneMessageWithManyRecipients)
                {
                    var recipients = groupsWithEmail.Select(g => new EmailOwnerRecipientData(g.Key) { Kind = tr.Kind }).ToList();

                    foreach (var list in TokenRecipientsCrossProduct(tokenRecipients, pos + 1))
                    {
                        var result = list.ToList();
                        result.InsertRange(0, recipients);
                        yield return result;
                    }
                }
                else
                {
                    throw new UnexpectedValueException(tr.WhenMany);
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
                entity != null ? new List<Filter> { new FilterCondition(QueryUtils.Parse("Entity", qd, 0), FilterOperation.EqualTo, entity!.ToLite()) } :
                throw new InvalidOperationException($"Impossible to create a Word report if '{nameof(entity)}' and '{nameof(model)}' are both null");

            filters.AddRange(template.Filters.ToFilterList());

            var orders = model?.GetOrders(qd) ?? new List<Order>();
            orders.AddRange(template.Orders.Select(qo => new Order(qo.Token.Token, qo.OrderType)).ToList());

            this.table = QueryLogic.Queries.ExecuteQuery(new QueryRequest
            {
                QueryName = queryName,
                GroupResults = template.GroupResults,
                Columns = columns,
                Pagination = model?.GetPagination() ?? new Pagination.All(),
                Filters = filters,
                Orders = orders,
            });

            this.dicTokenColumn = table.Columns.ToDictionary(rc => rc.Column.Token);

            this.currentRows = table.Rows;
        }
    }
}
