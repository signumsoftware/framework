using Signum.Entities.Mailing;

namespace Signum.Engine.Mailing.Senders;

public abstract class BaseEmailSender
{
    public virtual void Send(EmailMessageEntity email)
    {
        using (OperationLogic.AllowSave<EmailMessageEntity>())
        {
            if (!EmailLogic.Configuration.SendEmails)
            {
                email.State = EmailMessageState.Sent;
                email.Sent = Clock.Now;
                email.Save();
                return;
            }

            try
            {
                SendInternal(email);

                email.State = EmailMessageState.Sent;
                email.Sent = Clock.Now;
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

    protected abstract void SendInternal(EmailMessageEntity email);
}
