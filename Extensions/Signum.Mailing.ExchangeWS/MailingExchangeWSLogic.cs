using Signum.Mailing;

namespace Signum.MailingExchangeWS;

public static class MailingMicrosoftGraphLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            sb.Settings.AssertImplementedBy((EmailSenderConfigurationEntity o) => o.Service, typeof(ExchangeWebServiceEmailServiceEntity));

            EmailLogic.EmailSenders.Register((ExchangeWebServiceEmailServiceEntity s, EmailSenderConfigurationEntity c) => new ExchangeWebServiceSender(c, s));
        }
    }
}
