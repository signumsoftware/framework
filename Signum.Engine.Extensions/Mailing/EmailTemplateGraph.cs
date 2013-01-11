using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Mailing;
using Signum.Engine.Operations;
using Signum.Engine.Extensions.Properties;
using Signum.Utilities;

namespace Signum.Engine.Mailing
{
    public class EmailTemplateGraph : Graph<EmailTemplateDN, EmailTemplateState>
    {
        static bool registered;
        public static bool Registered { get { return registered; } }

        public static void Register()
        {
            GetState = t => t.State;

            new Construct(EmailTemplateOperation.Create)
            {
                ToState = EmailTemplateState.Created,
                Construct = _ => new EmailTemplateDN 
                { 
                    State = EmailTemplateState.Created,
                    From = EmailLogic.SenderManager.TryCC(m => m.DefaultFrom),
                    DisplayFrom = EmailLogic.SenderManager.TryCC(m => m.DefaultDisplayFrom),
                    SMTPConfiguration = EmailLogic.SenderManager.TryCC(m => m.DefaultSMTPConfiguration)
                }
            }.Register();

            new Execute(EmailTemplateOperation.Save)
            {
                ToState = EmailTemplateState.Modified,
                AllowsNew = true,
                Lite = false,
                FromStates = new[] { EmailTemplateState.Created, EmailTemplateState.Modified },
                Execute = (t, _) => t.State = EmailTemplateState.Modified
            }.Register();

            new Execute(EmailTemplateOperation.Enable) 
            {
                ToState = EmailTemplateState.Modified,
                FromStates = new[] { EmailTemplateState.Modified },
                CanExecute = t => t.Active ? Resources.TheTemplateIsAlreadyActive : null,
                Execute = (t, _) => t.Active = true
            }.Register();

            new Execute(EmailTemplateOperation.Disable) 
            {
                ToState = EmailTemplateState.Modified,
                FromStates = new[] { EmailTemplateState.Modified },
                CanExecute = t => !t.Active ? Resources.TheTemplateIsAlreadyInactive : null,
                Execute = (t, _) => t.Active = false
            }.Register();

            registered = true;
        }
    }
}
