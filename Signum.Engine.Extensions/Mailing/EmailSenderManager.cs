using System;
using System.Linq;
using System.Net.Mail;
using Signum.Engine.Basics;
using Signum.Utilities;
using Signum.Entities.Mailing;
using Signum.Entities;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Operations;
using Signum.Engine.Authorization;
using System.IO;
using Signum.Engine.Files;
using Microsoft.Exchange.WebServices.Data;

namespace Signum.Engine.Mailing
{
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
                        using (Transaction tr = Transaction.ForceNew())
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
}
