using System.Globalization;
using Signum.DynamicQuery.Tokens;
using Signum.Basics;
using Signum.Templating;
using Signum.UserAssets.Queries;
using Signum.Authorization;

namespace Signum.Mailing.Templates;


class EmailMessageBuilder
{
    EmailTemplateEntity template;
    Entity? entity;
    IEmailModel? model;
    EmailSenderConfigurationEntity? emailSenderConfig;
    CultureInfo? cultureInfo;

    QueryDescription? qd;
    QueryContext? queryContext;

    public EmailMessageBuilder(EmailTemplateEntity template, Entity? entity, IEmailModel? systemEmail, CultureInfo? cultureInfo)
    {
        this.template = template;
        this.entity = entity;
        this.model = systemEmail;

        var queryName = template.Query?.ToQueryName();
        this.qd = queryName == null ? null : QueryLogic.Queries.QueryDescription(queryName);
        this.emailSenderConfig = EmailTemplateLogic.GetSmtpConfiguration?.Invoke(template, (systemEmail?.UntypedEntity as Entity)?.ToLiteFat(), null);
        this.cultureInfo = cultureInfo;
    }




    public IEnumerable<EmailMessageEntity> CreateEmailMessageInternal()
    {
        if (this.qd != null)
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

            var context = new EmailTemplateLogic.GenerateAttachmentContext(template, ci)
            {
                QueryContext = this.queryContext,
                ModelType = template.Model?.ToType(),
                Model = model,
                Entity = entity,
            };

            email = new EmailMessageEntity
            {
                Target = entity?.ToLite() ?? (this.model?.UntypedEntity as Entity)?.ToLite(),
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
                    new TextTemplateParameters(entity, ci, queryContext)
                    {
                        IsHtml = false,
                        Model = model
                    });

                email.Body = new BigStringEmbedded(TextNode(message).Print(
                    new TextTemplateParameters(entity, ci, queryContext)
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
        {
            var subject = message.Subject
                .Split(new[] { "\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(l => l.Trim())
                .ToString(" ");

            message.SubjectParsedNode = TextTemplateParser.Parse(subject, qd, template.Model?.ToType());
        }

        return (TextTemplateParser.BlockNode)message.SubjectParsedNode;
    }

    IEnumerable<EmailFromEmbedded> GetFrom()
    {
        if (template.From != null)
        {
            if (template.From.AddressSource == EmailAddressSource.QueryToken)
            {
                var qc = this.queryContext!;
                ResultColumn owner = qc.ResultColumns.GetOrThrow(template.From.Token!.Token);
                var groups = qc.CurrentRows.GroupBy(r => (EmailOwnerData)r[owner]!).ToList();

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
                            if (emailSenderConfig != null && emailSenderConfig.DefaultFrom != null)
                                yield return emailSenderConfig.DefaultFrom.Clone();
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
                        using (this.queryContext!.OverrideRows(gr))
                        {
                            yield return new EmailFromEmbedded(gr.Key);
                        }
                    }
                }
            }
            else if (template.From.AddressSource == EmailAddressSource.HardcodedAddress)
            {
                yield return new EmailFromEmbedded
                {
                    EmailOwner = null,
                    EmailAddress = template.From.EmailAddress!,
                    DisplayName = template.From.DisplayName,
                    AzureUserId = template.From.AzureUserId,
                };
            }
            else if (template.From.AddressSource == EmailAddressSource.CurrentUser)
            {
                var user = UserEntity.Current.InDB(a => a.EmailOwnerData);

                yield return new EmailFromEmbedded
                {
                    EmailOwner = null,
                    EmailAddress = user.Email!,
                    DisplayName = user.DisplayName,
                    AzureUserId = user.AzureUserId,
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
            if (emailSenderConfig != null && emailSenderConfig.DefaultFrom != null)
            {
                yield return emailSenderConfig.DefaultFrom.Clone();
            }
            else
            {
                throw new InvalidOperationException("Not Default From found");
            }
        }
    }

    IEnumerable<List<EmailOwnerRecipientData>> GetRecipients()
    {
        foreach (List<EmailOwnerRecipientData> recipients in TokenRecipientsCrossProduct(template.Recipients.Where(a => a.AddressSource == EmailAddressSource.QueryToken).ToList(), 0))
        {
            recipients.AddRange(template.Recipients.Where(a => a.AddressSource != EmailAddressSource.QueryToken).Select(tr =>
            {
                var eod = 
                tr.AddressSource == EmailAddressSource.CurrentUser ? UserEntity.Current.InDB(a => a.EmailOwnerData) :
                tr.AddressSource == EmailAddressSource.HardcodedAddress ? new EmailOwnerData { Email = tr.EmailAddress!, DisplayName = tr.DisplayName } :
                throw new UnexpectedValueException(tr.AddressSource);

                return new EmailOwnerRecipientData(eod) { Kind = tr.Kind };
            }));

            if (model != null)
                recipients.AddRange(model.GetRecipients());

            if (emailSenderConfig != null)
            {
                recipients.AddRange(emailSenderConfig.AdditionalRecipients.Where(a => a.EmailOwner == null).Select(r =>
                    new EmailOwnerRecipientData(new EmailOwnerData { CultureInfo = null, DisplayName = r.DisplayName, Email = r.EmailAddress, Owner = r.EmailOwner }) { Kind = r.Kind }));
            }

            var validRecipients = recipients.Where(r => r.OwnerData.Email.HasText());
            if (validRecipients.Any())
                yield return validRecipients.ToList();
        }
    }


    private IEnumerable<List<EmailOwnerRecipientData>> TokenRecipientsCrossProduct(List<EmailTemplateRecipientEmbedded> tokenRecipients, int pos)
    {
        if (tokenRecipients.Count == pos)
            yield return new List<EmailOwnerRecipientData>();
        else
        {
            EmailTemplateRecipientEmbedded tr = tokenRecipients[pos];

            var qc = this.queryContext!;
            ResultColumn owner = qc.ResultColumns.GetOrThrow(tr.Token!.Token);

            var groups = qc.CurrentRows.GroupBy(r => (EmailOwnerData)r[owner]!).ToList();

            var groupsWithEmail = groups.Where(a => a.Key != null && a.Key.Email.HasText()).ToList();

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

                        using (this.queryContext!.OverrideRows(gr))
                        {
                            foreach (var list in TokenRecipientsCrossProduct(tokenRecipients, pos + 1))
                            {
                                var result = list.ToList();
                                result.Insert(0, rec);
                                yield return result;
                            }
                        }
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
        var qd = this.qd!;
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
                EmailTemplateLogic.FillAttachmentTokens.Invoke(a, new EmailTemplateLogic.FillAttachmentTokenContext(qd!, tokens)
                {
                    ModelType = template.Model?.ToType(),
                });
            }

            var columns = tokens.Distinct().Select(qt => new Column(qt, null)).ToList();

            var filters = model != null ? model.GetFilters(qd) :
                entity != null ? new List<Filter> { new FilterCondition(QueryUtils.Parse("Entity", qd, 0), FilterOperation.EqualTo, entity!.ToLite()) } :
                throw new InvalidOperationException($"Impossible to create a Word report if '{nameof(entity)}' and '{nameof(model)}' are both null");
            
            filters.AddRange(QueryFilterUtils.ToFilterList(template.Filters));

            var orders = model?.GetOrders(qd) ?? new List<Order>();
            orders.AddRange(template.Orders.Select(qo => new Order(qo.Token.Token, qo.OrderType)).ToList());

            var table = QueryLogic.Queries.ExecuteQuery(new QueryRequest
            {
                QueryName = qd.QueryName,
                GroupResults = template.GroupResults,
                Columns = columns,
                Pagination = model?.GetPagination() ?? new Pagination.All(),
                Filters = filters,
                Orders = orders,
            });

            this.queryContext = new QueryContext(qd, table);
        }
    }
}
