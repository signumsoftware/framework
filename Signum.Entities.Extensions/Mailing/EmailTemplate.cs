using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using System.ComponentModel;
using Signum.Entities.UserAssets;
using Signum.Entities.Templating;
using System.Xml.Linq;
using Signum.Entities.UserQueries;

namespace Signum.Entities.Mailing;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class EmailTemplateEntity : Entity, IUserAssetEntity
{
    public EmailTemplateEntity()
    {
        BindParent();
    }

    [UniqueIndex]
    public Guid Guid { get; set; } = Guid.NewGuid();

    public EmailTemplateEntity(object queryName) : this()
    {
        this.queryName = queryName;
    }

    [Ignore]
    internal object queryName;

    [UniqueIndex]
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    public bool EditableMessage { get; set; } = true;

    public bool DisableAuthorization { get; set; }
    
    public QueryEntity Query { get; set; }

    public EmailModelEntity? Model { get; set; }

    public EmailTemplateFromEmbedded? From { get; set; }

    [NoRepeatValidator]
    public MList<EmailTemplateRecipientEmbedded> Recipients { get; set; } = new MList<EmailTemplateRecipientEmbedded>();

    public bool GroupResults { get; set; }

    [PreserveOrder]
    public MList<QueryFilterEmbedded> Filters { get; set; } = new MList<QueryFilterEmbedded>();

    [PreserveOrder]
    public MList<QueryOrderEmbedded> Orders { get; set; } = new MList<QueryOrderEmbedded>();

    [PreserveOrder]
    [NoRepeatValidator, ImplementedBy(typeof(ImageAttachmentEntity)), BindParent]
    public MList<IAttachmentGeneratorEntity> Attachments { get; set; } = new MList<IAttachmentGeneratorEntity>();

    public Lite<EmailMasterTemplateEntity>? MasterTemplate { get; set; }

    public EmailMessageFormat MessageFormat { get; set; }

    [BindParent]
    public MList<EmailTemplateMessageEmbedded> Messages { get; set; } = new MList<EmailTemplateMessageEmbedded>();

    [BindParent]
    public TemplateApplicableEval? Applicable { get; set; }


    protected override string? PropertyValidation(System.Reflection.PropertyInfo pi)
    {
        if (pi.Name == nameof(Messages))
        {
            if (Messages == null || !Messages.Any())
                return EmailTemplateMessage.ThereAreNoMessagesForTheTemplate.NiceToString();

            if (Messages.GroupCount(m => m.CultureInfo).Any(c => c.Value > 1))
                return EmailTemplateMessage.TheresMoreThanOneMessageForTheSameLanguage.NiceToString();
        }

        return base.PropertyValidation(pi);
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);

    internal void ParseData(QueryDescription description)
    {
        var canAggregate = this.GroupResults ? SubTokensOptions.CanAggregate : 0;

        foreach (var r in Recipients.Where(r => r.Token != null))
            r.Token!.ParseData(this, description, SubTokensOptions.CanElement);

        if (From != null && From.Token != null)
            From.Token.ParseData(this, description, SubTokensOptions.CanElement);

        foreach (var f in Filters)
            f.ParseData(this, description, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | canAggregate);

        foreach (var o in Orders)
            o.ParseData(this, description, SubTokensOptions.CanElement | canAggregate);
    }

    public bool IsApplicable(Entity? entity)
    {
        if (Applicable == null)
            return true;

        try
        {
            return Applicable.Algorithm!.ApplicableUntyped(entity);
        }
        catch (Exception e)
        {
            throw new ApplicationException($"Error evaluating Applicable for EmailTemplate '{Name}' with entity '{entity}': " + e.Message, e);
        }
    }

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("EmailTemplate",
            new XAttribute("Name", Name),
            new XAttribute("Guid", Guid),
            new XAttribute("DisableAuthorization", DisableAuthorization),
            new XAttribute("Query", Query.Key),
            new XAttribute("EditableMessage", EditableMessage),
            Model == null ? null! /*FIX all null! -> null*/ : new XAttribute("Model", Model.FullClassName),
            MasterTemplate == null ? null! : new XAttribute("MasterTemplate", ctx.Include(MasterTemplate)),
            new XAttribute("GroupResults", GroupResults),
            Filters.IsNullOrEmpty() ? null! : new XElement("Filters", Filters.Select(f => f.ToXml(ctx)).ToList()),
            Orders.IsNullOrEmpty() ? null! : new XElement("Orders", Orders.Select(o => o.ToXml(ctx)).ToList()),
            new XAttribute("MessageFormat", MessageFormat),
            From == null ? null! : new XElement("From",
                From.DisplayName != null ? new XAttribute("DisplayName", From.DisplayName) : null!,
                From.EmailAddress != null ? new XAttribute("EmailAddress", From.EmailAddress) : null!,
                From.Token != null ? new XAttribute("Token", From.Token.Token.FullKey()) : null!,
                new XAttribute("WhenMany", From.WhenMany),
                new XAttribute("WhenNone", From.WhenNone)
                     ),
            new XElement("Recipients", Recipients.Select(rec =>
                new XElement("Recipient",
                     rec.DisplayName.HasText()? new XAttribute("DisplayName", rec.DisplayName) : null!,
                     rec.EmailAddress.HasText()? new XAttribute("EmailAddress", rec.EmailAddress) : null!,
                     new XAttribute("Kind", rec.Kind),
                     rec.Token != null ? new XAttribute("Token", rec.Token?.Token.FullKey()!) : null!,
                     new XAttribute("WhenMany", rec.WhenMany),
                     new XAttribute("WhenNone", rec.WhenNone)
                )
            )),
            Attachments.Any() ?  new XElement("Attachments", Attachments.Select(x => x.ToXml(ctx))) : null,
            new XElement("Messages", Messages.Select(x =>
                new XElement("Message",
                    new XAttribute("CultureInfo", x.CultureInfo.Name),
                    new XAttribute("Subject", x.Subject),
                    new XCData(x.Text)
                ))),

            this.Applicable?.Let(app => new XElement("Applicable", new XCData(app.Script)))!
        );
    }
       
    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        Guid = Guid.Parse(element.Attribute("Guid")!.Value);
        Name = element.Attribute("Name")!.Value;
        DisableAuthorization = element.Attribute("DisableAuthorization")?.Let(a => bool.Parse(a.Value)) ?? false;

        Query = ctx.GetQuery(element.Attribute("Query")!.Value);
        EditableMessage = bool.Parse(element.Attribute("EditableMessage")!.Value);
        Model = element.Attribute("Model")?.Let(at => ctx.GetEmailModel(at.Value));

        MasterTemplate = element.Attribute("MasterTemplate")?.Let(a => (Lite<EmailMasterTemplateEntity>)ctx.GetEntity(Guid.Parse(a.Value)).ToLiteFat());

        GroupResults = bool.Parse(element.Attribute("GroupResults")!.Value);
        Filters.Synchronize(element.Element("Filters")?.Elements().ToList(), (f, x) => f.FromXml(x, ctx));
        Orders.Synchronize(element.Element("Orders")?.Elements().ToList(), (o, x) => o.FromXml(x, ctx));

        MessageFormat = element.Attribute("MessageFormat")?.Value.ToEnum<EmailMessageFormat>() ??
            (element.Attribute("IsBodyHtml")?.Value.ToBool() == true ? EmailMessageFormat.HtmlComplex : EmailMessageFormat.PlainText);

        From = From.CreateOrAssignEmbedded(element.Element("From"), (etf, xml) => 
        {
            etf.DisplayName = xml.Attribute("DisplayName")?.Value;
            etf.EmailAddress = xml.Attribute("EmailAddress")?.Value;
            etf.Token = xml.Attribute("Token")?.Let(t => new QueryTokenEmbedded(t.Value));
            etf.WhenMany = xml.Attribute("WhenMany")?.Value.ToEnum<WhenManyFromBehaviour>() ?? WhenManyFromBehaviour.FistResult;
            etf.WhenNone = xml.Attribute("WhenNone")?.Value.ToEnum<WhenNoneFromBehaviour>() ?? WhenNoneFromBehaviour.NoMessage;
        });

        Recipients.Synchronize(element.Element("Recipients")!.Elements("Recipient").ToList(), (rep, xml) => 
        {
            rep.DisplayName = xml.Attribute("DisplayName")?.Value;
            rep.EmailAddress = xml.Attribute("EmailAddress")?.Value;
            rep.Kind = xml.Attribute("Kind")!.Value.ToEnum<EmailRecipientKind>();
            rep.Token = xml.Attribute("Token")?.Let(a => new QueryTokenEmbedded(a.Value));
            rep.WhenMany = xml.Attribute("WhenMany")?.Value?.ToEnum<WhenManyRecipiensBehaviour>() ?? WhenManyRecipiensBehaviour.KeepOneMessageWithManyRecipients;
            rep.WhenNone = xml.Attribute("WhenNone")?.Value?.ToEnum<WhenNoneRecipientsBehaviour>() ?? WhenNoneRecipientsBehaviour.ThrowException;
        });

        Messages.Synchronize(element.Element("Messages")!.Elements("Message").ToList(), (et, xml) =>
        {
            et.CultureInfo = ctx.GetCultureInfoEntity(xml.Attribute("CultureInfo")!.Value);
            et.Subject = xml.Attribute("Subject")!.Value;
            et.Text = xml.Value;
        });

        Attachments.SynchronizeAttachments(element.Element("Attachments")?.Elements().ToList(), ctx, this);

        Applicable = element.Element("Applicable")?.Let(app => new TemplateApplicableEval { Script =  app.Value});
        ParseData(ctx.GetQueryDescription(Query));
    }

}


public abstract class EmailTemplateAddressEmbedded : EmbeddedEntity
{
    public string? EmailAddress { get; set; }

    public string? DisplayName { get; set; }

    public QueryTokenEmbedded? Token { get; set; }


    public override string ToString()
    {
        return "{0} <{1}>".FormatWith(DisplayName, EmailAddress);
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(Token))
        {
            if (Token == null && EmailAddress.IsNullOrEmpty())
                return EmailTemplateMessage.TokenOrEmailAddressMustBeSet.NiceToString();

            if (Token != null && !EmailAddress.IsNullOrEmpty())
                return EmailTemplateMessage.TokenAndEmailAddressCanNotBeSetAtTheSameTime.NiceToString();

            if (Token != null && Token.Token.Type != typeof(EmailOwnerData))
                return EmailTemplateMessage.TokenMustBeA0.NiceToString(typeof(EmailOwnerData).NiceName());
        }

        return null;
    }

}


public class EmailTemplateRecipientEmbedded : EmailTemplateAddressEmbedded
{
    public EmailRecipientKind Kind { get; set; }

    public WhenNoneRecipientsBehaviour WhenNone { get; set; }
    public WhenManyRecipiensBehaviour WhenMany { get; set; }

    public override string ToString()
    {
        return "{0} {1} <{2}>".FormatWith(Kind.NiceToString(), DisplayName, EmailAddress);
    }
}

public enum EmailMessageFormat
{
    [Description("Plain Text")]
    PlainText,
    [Description("HTML (Complex)")]
    HtmlComplex,
    [Description("HTML (Simple)")]
    HtmlSimple
}

public enum WhenNoneRecipientsBehaviour
{
    ThrowException,
    NoMessage,
    NoRecipients
}

public enum WhenManyRecipiensBehaviour
{
    SplitMessages,
    KeepOneMessageWithManyRecipients,
}

public class EmailTemplateFromEmbedded : EmailTemplateAddressEmbedded
{
    public WhenNoneFromBehaviour WhenNone { get; set; }
    public WhenManyFromBehaviour WhenMany { get; set; }

    public Guid? AzureUserId { get; set; }
}


public enum WhenNoneFromBehaviour
{
    ThrowException,
    NoMessage,
    DefaultFrom
}

public enum WhenManyFromBehaviour
{
    SplitMessages,
    FistResult,
}


public class EmailTemplateMessageEmbedded : EmbeddedEntity
{
    public EmailTemplateMessageEmbedded() { }

    public EmailTemplateMessageEmbedded(CultureInfoEntity culture)
    {
        this.CultureInfo = culture;
    }

    
    public CultureInfoEntity CultureInfo { get; set; }

    [DbType(Size = int.MaxValue)]
    string text;
    [StringLengthValidator(MultiLine=true)]
    public string Text
    {
        get { return text; }
        set
        {
            if (Set(ref text, value))
                TextParsedNode = null;
        }
    }

    [Ignore]
    internal object? TextParsedNode;

    string subject;
    [StringLengthValidator(Min = 3, Max = 200)]
    public string Subject
    {
        get { return subject; }
        set
        {
            if (Set(ref subject, value))
                SubjectParsedNode = null;
        }
    }

    [Ignore]
    internal object? SubjectParsedNode;

    public override string ToString()
    {
        return CultureInfo?.ToString() ?? EmailTemplateMessage.NewCulture.NiceToString();
    }
 }

public static class AttachmentFromXmlExtensions
{
    public static Dictionary<string, Type> TypeMapping = new Dictionary<string, Type>();
    public static void SynchronizeAttachments(this MList<IAttachmentGeneratorEntity> entities, List<XElement>? xElements, IFromXmlContext ctx, IUserAssetEntity userAsset)
    {
        if (xElements == null)
            xElements = new List<XElement>();

        for (int i = 0; i < xElements.Count; i++)
        {
            IAttachmentGeneratorEntity entity;
            var type = TypeMapping.GetOrThrow<string, Type>(xElements[i].Name.LocalName);

            if (entities.Count == i)
            {
                entity = (IAttachmentGeneratorEntity)Activator.CreateInstance(type)!;
                entities.Add(entity);
            }
            else if(entities[i].GetType() != type)
            {
                entity = entities[i] = (IAttachmentGeneratorEntity)Activator.CreateInstance(type)!;
            }
            else
                entity = entities[i];

            entity.FromXml(xElements[i], ctx, userAsset);
        }

        if (entities.Count > xElements.Count)
        {
            entities.RemoveRange(xElements.Count, entities.Count - xElements.Count);
        }
    }
}


public interface IAttachmentGeneratorEntity : IEntity
{
    XElement ToXml(IToXmlContext ctx);

    void FromXml(XElement element, IFromXmlContext ctx, IUserAssetEntity userAsset);
}

[AutoInit]
public static class EmailTemplateOperation
{
    public static ConstructSymbol<EmailTemplateEntity>.From<EmailModelEntity> CreateEmailTemplateFromModel;
    public static ConstructSymbol<EmailTemplateEntity>.Simple Create;
    public static ExecuteSymbol<EmailTemplateEntity> Save;
    public static DeleteSymbol<EmailTemplateEntity> Delete;
}

public enum EmailTemplateMessage
{
    [Description("End date must be higher than start date")]
    EndDateMustBeHigherThanStartDate,
    [Description("There are no messages for the template")]
    ThereAreNoMessagesForTheTemplate,
    [Description("There must be a message for {0}")]
    ThereMustBeAMessageFor0,
    [Description("There's more than one message for the same language")]
    TheresMoreThanOneMessageForTheSameLanguage,
    [Description("The text must contain {0} indicating replacement point")]
    TheTextMustContain0IndicatingReplacementPoint,
    [Description("Impossible to access {0} because the template has no {1}")]
    ImpossibleToAccess0BecauseTheTemplateHAsNo1,
    NewCulture,
    TokenOrEmailAddressMustBeSet,
    TokenAndEmailAddressCanNotBeSetAtTheSameTime,
    [Description("Token must be a {0}")]
    TokenMustBeA0,
    ShowPreview,
    HidePreview
}

public enum EmailTemplateViewMessage
{
    [Description("Insert message content")]
    InsertMessageContent,
    [Description("Insert")]
    Insert,
    [Description("Language")]
    Language
}

[InTypeScript(true)]
public enum EmailTemplateVisibleOn
{
    Single = 1,
    Multiple = 2,
    Query = 4
}
