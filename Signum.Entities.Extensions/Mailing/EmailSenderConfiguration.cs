using System.Net.Mail;
using Microsoft.Exchange.WebServices.Data;
using System.ComponentModel;

namespace Signum.Entities.Mailing;

[EntityKind(EntityKind.Shared, EntityData.Master)]
public class EmailSenderConfigurationEntity : Entity
{
    static EmailSenderConfigurationEntity()
    {
        DescriptionManager.ExternalEnums.Add(typeof(SmtpDeliveryFormat), m => m.Name);
        DescriptionManager.ExternalEnums.Add(typeof(SmtpDeliveryMethod), m => m.Name);
        DescriptionManager.ExternalEnums.Add(typeof(ExchangeVersion), m => m.Name);
    }

    [UniqueIndex]
    [StringLengthValidator(Min = 1, Max = 100)]
    public string Name { get; set; }

    [BindParent]
    public EmailFromEmbedded? DefaultFrom { get; set; }

    [NoRepeatValidator]
    public MList<EmailRecipientEmbedded> AdditionalRecipients { get; set; } = new MList<EmailRecipientEmbedded>();

    public SmtpEmbedded? SMTP { get; set; }

    public ExchangeWebServiceEmbedded? Exchange { get; set; }

    public MicrosoftGraphEmbedded? MicrosoftGraph { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(SMTP) || pi.Name == nameof(Exchange) || pi.Name == nameof(MicrosoftGraph))
        {
            var count = new[] { SMTP != null, Exchange != null, MicrosoftGraph != null }.Count(a => a);

            if (count == 0)
                return ValidationMessage._0Or1ShouldBeSet.NiceToString(
                    NicePropertyName(() => SMTP) + ", " + NicePropertyName(() => Exchange),
                    NicePropertyName(() => MicrosoftGraph));


            if (count > 1)
                return ValidationMessage._0And1CanNotBeSetAtTheSameTime.NiceToString(
                    NicePropertyName(() => SMTP) + ", " + NicePropertyName(() => Exchange),
                    NicePropertyName(() => MicrosoftGraph));
        }

        return base.PropertyValidation(pi);
    }

    protected override string? ChildPropertyValidation(ModifiableEntity sender, PropertyInfo pi)
    {
        if (sender == DefaultFrom && pi.Name == nameof(DefaultFrom.AzureUserId))
        {
            if (DefaultFrom.AzureUserId == null && MicrosoftGraph != null)
                return ValidationMessage._0IsMandatoryWhen1IsSet.NiceToString(pi.NiceName(), NicePropertyName(() => MicrosoftGraph));
        }

        return base.ChildPropertyValidation(sender, pi);
    }
}


[AutoInit]
public static class EmailSenderConfigurationOperation
{
    public static ExecuteSymbol<EmailSenderConfigurationEntity> Save;
}

public class SmtpEmbedded : EmbeddedEntity
{
    public SmtpDeliveryFormat DeliveryFormat { get; set; }

    public SmtpDeliveryMethod DeliveryMethod { get; set; }

    public SmtpNetworkDeliveryEmbedded? Network { get; set; }

    [StringLengthValidator(Min = 3, Max = 300), FileNameValidator]
    public string? PickupDirectoryLocation { get; set; }

    static StateValidator<SmtpEmbedded, SmtpDeliveryMethod> stateValidator = new StateValidator<SmtpEmbedded, SmtpDeliveryMethod>(
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

    public bool UseDefaultCredentials { get; set; } = true;

    public bool EnableSSL { get; set; }

    
    public MList<ClientCertificationFileEmbedded> ClientCertificationFiles { get; set; } = new MList<ClientCertificationFileEmbedded>();
}

public class ClientCertificationFileEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Min = 2, Max = 300),]
    public string FullFilePath { get; set; }

    public CertFileType CertFileType { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => FullFilePath);
}

public enum CertFileType
{
    CertFile,
    SignedFile
}

public class ExchangeWebServiceEmbedded : EmbeddedEntity
{
    public ExchangeVersion ExchangeVersion { get; set; }

    [StringLengthValidator(Max = 300)]
    public string? Url { get; set; }


    [StringLengthValidator(Max = 100)]
    public string? Username { get; set; }

    [StringLengthValidator(Max = 100), Format(FormatAttribute.Password)]
    public string? Password { get; set; }

    public bool UseDefaultCredentials { get; set; } = true;
}

public class MicrosoftGraphEmbedded : EmbeddedEntity
{
    public bool UseActiveDirectoryConfiguration { get; set; }

    [Description("Azure Application (client) ID")]
    public Guid? Azure_ApplicationID { get; set; }

    [Description("Azure Directory (tenant) ID")]
    public Guid? Azure_DirectoryID { get; set; }

    [StringLengthValidator(Max = 100), Description("Azure Client Secret Value")]
    public string? Azure_ClientSecret { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (!UseActiveDirectoryConfiguration)
        {
            if (pi.Name == nameof(Azure_ApplicationID) && Azure_ApplicationID == null)
                return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

            if (pi.Name == nameof(Azure_DirectoryID) && Azure_DirectoryID == null)
                return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

            if (pi.Name == nameof(Azure_ClientSecret) && !Azure_ClientSecret.HasText())
                return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());
        }

        return base.PropertyValidation(pi);
    }
}
