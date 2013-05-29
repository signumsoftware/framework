using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Mailing;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Engine.Mailing
{
    public class EmailGraph : Graph<EmailMessageDN, EmailMessageState>
    {
        public static void Register() 
        {
            GetState = m => m.State;

            new Construct(EmailMessageOperation.CreateMail)
            {
                Construct = _ => new EmailMessageDN 
                {
                    State = EmailMessageState.Created,
                }
            }.Register();

            new ConstructFrom<IIdentifiable>(EmailMessageOperation.CreateMailFromTemplate)
            {
                AllowsNew = false,
                Construct = (e, args) =>
                {
                    var template = args.GetArg<Lite<EmailTemplateDN>>();
                    return EmailLogic.CreateEmailMessage(template.Retrieve(), e);
                }
            }.Register();

            new Execute(EmailMessageOperation.Send) 
            {
                CanExecute = m => m.State == EmailMessageState.Created ? null : EmailMessageMessage.TheEmailMessageCannotBeSentFromState0.NiceToString().Formato(m.State.NiceToString()),
                AllowsNew = true,
                Lite = false,
                Execute = (m, _) => EmailLogic.SenderManager.Send(m)
            }.Register();

            new ConstructFrom<EmailMessageDN>(EmailMessageOperation.ReSend)
            {
                AllowsNew = false,
                Construct = (m, _) => new EmailMessageDN 
                {
                    From = m.From.Clone(),
                    Recipients  = m.Recipients.Select(r=>r.Clone()).ToMList(),
                    Subject = m.Subject,
                    Text = m.Text,
                    IsBodyHtml = m.IsBodyHtml,
                    Template = m.Template,
                    EditableMessage = m.EditableMessage,
                    State = EmailMessageState.Created
                }
            }.Register();
        }
    }
}
