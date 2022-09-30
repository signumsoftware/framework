using Signum.Entities.Basics;
using System.ComponentModel;
using Signum.Entities.UserAssets;
using Signum.Entities.DynamicQuery;

namespace Signum.Entities.SMS;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class SMSTemplateEntity : Entity
{
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    public bool Certified { get; set; }

    public static bool DefaultEditableMessage = true;
    public bool EditableMessage { get; set; } = DefaultEditableMessage;

    public bool DisableAuthorization { get; set; }

    public QueryEntity? Query { get; set; }

    public SMSModelEntity? Model { get; set; }

    [BindParent]
    public MList<SMSTemplateMessageEmbedded> Messages { get; set; } = new MList<SMSTemplateMessageEmbedded>();

    [StringLengthValidator(Max = 200)]
    public string? From { get; set; }

    public QueryTokenEmbedded? To { get; set; }

    public MessageLengthExceeded MessageLengthExceeded { get; set; } = MessageLengthExceeded.NotAllowed;

    public static bool DefaultRemoveNoSMSCharacters = false;
    public bool RemoveNoSMSCharacters { get; set; } = DefaultRemoveNoSMSCharacters;

    public static bool DefaultIsActive = false;
    public bool IsActive { get; set; } = DefaultIsActive;

    protected override string? PropertyValidation(System.Reflection.PropertyInfo pi)
    {
        if (pi.Name == nameof(To) && To == null && (Query != null || Model != null))
        {
            return SMSTemplateMessage.ToMustBeSetInTheTemplate.NiceToString();
        }

        if (pi.Name == nameof(Messages))
        {
            if (Messages == null || !Messages.Any())
                return SMSTemplateMessage.ThereAreNoMessagesForTheTemplate.NiceToString();

            if (Messages.GroupCount(m => m.CultureInfo).Any(c => c.Value > 1))
                return SMSTemplateMessage.TheresMoreThanOneMessageForTheSameLanguage.NiceToString();
        }

        return base.PropertyValidation(pi);
    }

    internal void ParseData(QueryDescription queryDescription)
    {
        if (To != null)
            To.ParseData(this, queryDescription, 0);
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);
    
}

[AutoInit]
public static class SMSTemplateOperation
{
    public static ConstructSymbol<SMSTemplateEntity>.From<SMSModelEntity> CreateSMSTemplateFromModel;
    public static ConstructSymbol<SMSTemplateEntity>.Simple Create;
    public static ExecuteSymbol<SMSTemplateEntity> Save;

}

public enum MessageLengthExceeded
{
    NotAllowed,
    Allowed,
    TextPruning,
}

public class SMSTemplateMessageEmbedded : EmbeddedEntity
{
    public SMSTemplateMessageEmbedded() { }

    public SMSTemplateMessageEmbedded(CultureInfoEntity culture)
    {
        this.CultureInfo = culture;
    }
    
    public CultureInfoEntity CultureInfo { get; set; }

    [StringLengthValidator(MultiLine = true)]
    public string Message { get; set; }

    public override string ToString()
    {
        return CultureInfo?.ToString() ?? SMSTemplateMessage.NewCulture.NiceToString();
    }
}

public enum SMSTemplateMessage
{
    [Description("There are no messages for the template")]
    ThereAreNoMessagesForTheTemplate,
    [Description("There must be a message for {0}")]
    ThereMustBeAMessageFor0,
    [Description("There's more than one message for the same language")]
    TheresMoreThanOneMessageForTheSameLanguage,
    NewCulture,
    [Description("{0} characters remaining (before replacements)")]
    _0CharactersRemainingBeforeReplacements,
    ToMustBeSetInTheTemplate
}


[EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false)]
public class SMSModelEntity : Entity
{
    [UniqueIndex]
    public string FullClassName { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => FullClassName);
}

public interface ISMSOwnerEntity : IEntity
{

}

[DescriptionOptions(DescriptionOptions.Description | DescriptionOptions.Members)]
public class SMSOwnerData : IEquatable<SMSOwnerData>
{
    public Lite<ISMSOwnerEntity>? Owner { get; set; }
    public string TelephoneNumber { get; set; }
    public CultureInfoEntity? CultureInfo { get; set; }

    public override bool Equals(object? obj) => obj is SMSOwnerData sms && Equals(sms);
    public bool Equals(SMSOwnerData? other)
    {
        return Owner != null && other != null && other.Owner != null && Owner.Equals(other.Owner);
    }

    public override int GetHashCode()
    {
        return Owner == null ? base.GetHashCode() : Owner.GetHashCode();
    }

    public override string ToString()
    {
        return "{0} ({1})".FormatWith(TelephoneNumber, Owner);
    }
}
