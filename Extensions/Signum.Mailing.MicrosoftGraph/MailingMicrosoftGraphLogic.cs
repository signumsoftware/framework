using Signum.Mailing;

namespace Signum.Mailing.MicrosoftGraph;

public static class MailingMicrosoftGraphLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Settings.AssertImplementedBy((EmailSenderConfigurationEntity o) => o.Service, typeof(MicrosoftGraphEmailServiceEntity));

        EmailLogic.EmailSenders.Register((MicrosoftGraphEmailServiceEntity s, EmailSenderConfigurationEntity c) => new MicrosoftGraphSender(c, s));
    }
}
