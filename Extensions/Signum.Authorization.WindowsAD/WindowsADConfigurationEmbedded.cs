using Signum.Authorization.BaseAD;

namespace Signum.Authorization.WindowsAD;

public class WindowsADConfigurationEmbedded : BaseADConfigurationEmbedded
{
    public bool LoginWithWindowsAuthenticator { get; set; }

    public bool LoginWithActiveDirectoryRegistry { get; set; }

    [StringLengthValidator(Max = 200)]
    public string? DomainName { get; set; }

    public string? DirectoryRegistry_Username { get; set; }

    [Format(FormatAttribute.Password)]
    public string? DirectoryRegistry_Password { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (LoginWithWindowsAuthenticator || LoginWithActiveDirectoryRegistry)
        {
            if (pi.Name == nameof(DomainName) && !DomainName.HasText())
                return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());
        }

        return base.PropertyValidation(pi);
    }
}
