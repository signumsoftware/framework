using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Mailing;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Utilities;
using Signum.Engine.Extensions.Properties;

namespace Signum.Engine.Mailing
{
    public class EmailGraph : Graph<EmailMessageDN, EmailState>
    {
        public static void Register() 
        {
            GetState = m => m.State;

            new BasicConstruct<EmailMessageDN>(EmailOperations.CreateMail)
            {
                Construct = _ => new EmailMessageDN 
                {
                    State = EmailState.Created,
                    From = EmailLogic.SenderManager.TryCC(m => m.DefaultFrom),
                    DisplayFrom = EmailLogic.SenderManager.TryCC(m => m.DefaultDisplayFrom),
                }
            }.Register();

            new BasicConstructFrom<IIdentifiable, EmailMessageDN>(EmailOperations.CreateMailFromTemplate)
            {
                AllowsNew = false,
                Construct = (e, args) =>
                {
                    var template = args.GetArg<Lite<EmailTemplateDN>>(0);
                    return EmailLogic.CreateEmailMessage(e, template.Retrieve());
                }
            }.Register();

            new BasicExecute<EmailMessageDN>(EmailOperations.Send) 
            {
                CanExecute = m => m.State == EmailState.Created ? null : Resources.TheEmailMessageCanNotBeSendFromState0.Formato(m.State.NiceToString()),
                AllowsNew = true,
                Lite = false,
                Execute = (m, _) => EmailLogic.SenderManager.Send(m)
            }.Register();

            new BasicConstructFrom<EmailMessageDN, EmailMessageDN>(EmailOperations.ReSend)
            {
                AllowsNew = false,
                Construct = (m, _) => new EmailMessageDN 
                {
                    Bcc = m.Bcc,
                    Text = m.Text,
                    Cc = m.Cc,
                    DisplayFrom = m.DisplayFrom,
                    EditableMessage = m.EditableMessage,
                    From = m.From,
                    IsBodyHtml = m.IsBodyHtml,
                    Recipient = m.Recipient,
                    Subject = m.Subject,
                    Template = m.Template,
                    To = m.To,
                    State = EmailState.Created
                }
            }.Register();
        }
    }
}
