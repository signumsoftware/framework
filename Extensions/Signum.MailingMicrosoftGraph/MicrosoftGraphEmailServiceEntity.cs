using Signum.Mailing;
using System.ComponentModel;

namespace Signum.MailingMicrosoftGraph;


[EntityKind(EntityKind.Part, EntityData.Master)]
public class MicrosoftGraphEmailServiceEntity : EmailServiceEntity
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


    public override MicrosoftGraphEmailServiceEntity Clone()
    {
        return new MicrosoftGraphEmailServiceEntity
        {
            UseActiveDirectoryConfiguration = UseActiveDirectoryConfiguration,
            Azure_ApplicationID = Azure_ApplicationID,
            Azure_DirectoryID = Azure_DirectoryID,
            Azure_ClientSecret = Azure_ClientSecret,
        };
    }


    

    //override ValidateFrom()
    //{
    //    ValidationMessage._0IsMandatoryWhen1IsSet.NiceToString(pi.NiceName(), NicePropertyName(() => microsoftGraph))
    //}
}
