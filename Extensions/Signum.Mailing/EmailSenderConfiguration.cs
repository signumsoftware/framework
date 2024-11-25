using System.Net.Mail;

namespace Signum.Mailing;

[EntityKind(EntityKind.Shared, EntityData.Master)]
public class EmailSenderConfigurationEntity : Entity
{
    static EmailSenderConfigurationEntity()
    {
        DescriptionManager.ExternalEnums.Add(typeof(SmtpDeliveryFormat), m => m.Name);
        DescriptionManager.ExternalEnums.Add(typeof(SmtpDeliveryMethod), m => m.Name);

        AssemblySchemaNameAttribute.OverridenAssembly.Add(typeof(SmtpDeliveryFormat), typeof(EmailSenderConfigurationEntity).Assembly!);
        AssemblySchemaNameAttribute.OverridenAssembly.Add(typeof(SmtpDeliveryMethod), typeof(EmailSenderConfigurationEntity).Assembly!);
    }

    [UniqueIndex]
    [StringLengthValidator(Min = 1, Max = 100)]
    public string Name { get; set; }

    [BindParent]
    public EmailFromEmbedded? DefaultFrom { get; set; }

    [NoRepeatValidator]
    public MList<EmailRecipientEmbedded> AdditionalRecipients { get; set; } = new MList<EmailRecipientEmbedded>();

    [ImplementedBy(typeof(SmtpEmailServiceEntity))]
    public EmailServiceEntity Service { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);

    protected override string? ChildPropertyValidation(ModifiableEntity sender, PropertyInfo pi)
    {
        if (sender == DefaultFrom && pi.Name == nameof(DefaultFrom.AzureUserId))
        {
            if (Service != null )
                return Service.ValidateFrom(DefaultFrom, pi);
        }

        return base.ChildPropertyValidation(sender, pi);
    }

    public EmailSenderConfigurationEntity Clone()
    {
        return new EmailSenderConfigurationEntity
        {
            DefaultFrom = DefaultFrom?.Clone(),
            AdditionalRecipients = AdditionalRecipients.ToMList(),
            Service = Service
        };
    }
}


[AutoInit]
public static class EmailSenderConfigurationOperation
{
    public static readonly ExecuteSymbol<EmailSenderConfigurationEntity> Save;
    public static readonly ConstructSymbol<EmailSenderConfigurationEntity>.From<EmailSenderConfigurationEntity> Clone;
}

public abstract class EmailServiceEntity : Entity
{
    public abstract EmailServiceEntity Clone();

    public virtual string? ValidateFrom(EmailFromEmbedded from, PropertyInfo pi) => null;
}

[EntityKind(EntityKind.Part, EntityData.Master)]
public class SmtpEmailServiceEntity : EmailServiceEntity
{


    public SmtpDeliveryFormat DeliveryFormat { get; set; }

    public SmtpDeliveryMethod DeliveryMethod { get; set; }

    public SmtpNetworkDeliveryEmbedded? Network { get; set; }

    [StringLengthValidator(Min = 3, Max = 300), FileNameValidator]
    public string? PickupDirectoryLocation { get; set; }

    static StateValidator<SmtpEmailServiceEntity, SmtpDeliveryMethod> stateValidator = new StateValidator<SmtpEmailServiceEntity, SmtpDeliveryMethod>(
       a => a.DeliveryMethod, a => a.Network, a => a.PickupDirectoryLocation)
        {
            {SmtpDeliveryMethod.Network,        true, null },
            {SmtpDeliveryMethod.SpecifiedPickupDirectory, null, true},
            {SmtpDeliveryMethod.PickupDirectoryFromIis,    null, null },
        };

    protected override string? PropertyValidation(System.Reflection.PropertyInfo pi)
    {
        return stateValidator.Validate(this, pi) ?? base.PropertyValidation(pi);
    }

    public override SmtpEmailServiceEntity Clone()
    {
        return new SmtpEmailServiceEntity 
        { 
            DeliveryFormat = DeliveryFormat,
            DeliveryMethod = DeliveryMethod,
            Network = Network?.Clone(),
            PickupDirectoryLocation = PickupDirectoryLocation
        };
    }
}

public class SmtpNetworkDeliveryEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Host { get; set; }

    public int Port { get; set; } = 25;

    [StringLengthValidator(Max = 100)]
    public string? Username { get; set; }

    [StringLengthValidator(Max = 100), Format(FormatAttribute.Password)]
    public string? Password { get; set; }
    
    [Ignore, InTypeScript(true)]
    public string? NewPassword { get; set; }

    public bool UseDefaultCredentials { get; set; } = true;

    public bool EnableSSL { get; set; }

    
    public MList<ClientCertificationFileEmbedded> ClientCertificationFiles { get; set; } = new MList<ClientCertificationFileEmbedded>();

    public SmtpNetworkDeliveryEmbedded Clone()
    {
        return new SmtpNetworkDeliveryEmbedded
        {
            Host = Host,
            Port = Port,
            Username = Username,
            Password = Password,
            UseDefaultCredentials = UseDefaultCredentials,
            EnableSSL = EnableSSL
        };
    }

}

public class ClientCertificationFileEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Min = 2, Max = 300),]
    public string FullFilePath { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => FullFilePath);
}

