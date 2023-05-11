using Signum.Mailing;

namespace Signum.Mailing.MicrosoftGraph;

public static class MailingMicrosoftGraphLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            sb.Settings.AssertImplementedBy((EmailSenderConfigurationEntity o) => o.Service, typeof(MicrosoftGraphEmailServiceEntity));

            EmailLogic.EmailSenders.Register((MicrosoftGraphEmailServiceEntity s, EmailSenderConfigurationEntity c) => new MicrosoftGraphSender(c, s));
        }
    }
}
