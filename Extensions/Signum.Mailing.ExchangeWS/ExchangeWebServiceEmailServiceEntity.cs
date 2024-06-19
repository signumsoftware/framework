using Microsoft.Exchange.WebServices.Data;
using Signum.Mailing;

namespace Signum.Mailing.ExchangeWS;

[EntityKind(EntityKind.Part, EntityData.Master)]
public class ExchangeWebServiceEmailServiceEntity : EmailServiceEntity
{
    public ExchangeVersion ExchangeVersion { get; set; }

    [StringLengthValidator(Max = 300)]
    public string? Url { get; set; }

    [StringLengthValidator(Max = 100)]
    public string? Username { get; set; }

    [StringLengthValidator(Max = 100), Format(FormatAttribute.Password)]
    public string? Password { get; set; }

    public bool UseDefaultCredentials { get; set; } = true;

    public override ExchangeWebServiceEmailServiceEntity Clone()
    {
        return new ExchangeWebServiceEmailServiceEntity
        {
            ExchangeVersion = ExchangeVersion,
            Url = Url,
            Username = Username,
            Password = Password,
            UseDefaultCredentials = UseDefaultCredentials,
        };

    }
}
