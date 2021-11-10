using Signum.Entities.Mailing;

namespace Signum.Engine.Mailing;

public partial class EmailSenderManager : IEmailSenderManager
{
    private Func<EmailTemplateEntity?, Lite<Entity>?, EmailMessageEntity?, EmailSenderConfigurationEntity> getEmailSenderConfiguration;

    public EmailSenderManager(Func<EmailTemplateEntity?, Lite<Entity>?, EmailMessageEntity?, EmailSenderConfigurationEntity> getEmailSenderConfiguration)
    {
        this.getEmailSenderConfiguration = getEmailSenderConfiguration;
    }

    public virtual void Send(EmailMessageEntity email)
    {
        using (OperationLogic.AllowSave<EmailMessageEntity>())
        {
            if (!EmailLogic.Configuration.SendEmails)
            {
                email.State = EmailMessageState.Sent;
                email.Sent = TimeZoneManager.Now;
                email.Save();
                return;
            }

            try
            {
                SendInternal(email);

                email.State = EmailMessageState.Sent;
                email.Sent = TimeZoneManager.Now;
                email.Save();
            }
            catch (Exception ex)
            {
                if (Transaction.InTestTransaction) //Transaction.IsTestTransaction
                    throw;
                var exLog = ex.LogException().ToLite();

                try
                {
                    using (var tr = Transaction.ForceNew())
                    {
                        email.Exception = exLog;
                        email.State = EmailMessageState.SentException;
                        email.Save();

                        tr.Commit();
                    }
                }
                catch { } //error updating state for email  

                throw;
            }
        }
    }

    protected virtual void SendInternal(EmailMessageEntity email)
    {
        var template = email.Template?.Try(t => EmailTemplateLogic.EmailTemplatesLazy.Value.GetOrThrow(t));

        var config = getEmailSenderConfiguration(template, email.Target, email);

        if (config.SMTP != null)
        {
            SendSMTP(email, config.SMTP);
        }
        else if (config.Exchange != null)
        {
            SendExchangeWebService(email, config.Exchange);
        }
        else if (config.MicrosoftGraph != null)
        {
            SendMicrosoftGraph(email, config.MicrosoftGraph);
        }
        else
            throw new InvalidOperationException("No way to send email found");
    }
}
