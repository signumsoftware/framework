using Microsoft.Exchange.WebServices.Data;
using Signum.Mailing;

namespace Signum.Mailing.ExchangeWS;

public static class MailingMicrosoftGraphLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            sb.Settings.AssertImplementedBy((EmailSenderConfigurationEntity o) => o.Service, typeof(ExchangeWebServiceEmailServiceEntity));

            EmailLogic.EmailSenders.Register((ExchangeWebServiceEmailServiceEntity s, EmailSenderConfigurationEntity c) => new ExchangeWebServiceSender(c, s));

            DescriptionManager.ExternalEnums.Add(typeof(ExchangeVersion), m => m.Name);

            if (sb.WebServerBuilder != null)
                MailingExchangeWSServer.Start(sb.WebServerBuilder.WebApplication);
        }
    }
}
