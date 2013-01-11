using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Mailing;
using Signum.Engine.Operations;

namespace Signum.Engine.Mailing
{
    public class EmailMasterTemplateGraph : Graph<EmailMasterTemplateDN, EmailTemplateState>
    {
        public static void Register()
        {
            GetState = t => t.State;

            new Construct(EmailMasterTemplateOperation.Create)
            {
                ToState = EmailTemplateState.Created,
                Construct = _ => new EmailMasterTemplateDN { State = EmailTemplateState.Created }
            }.Register();

            new BasicExecute<EmailMasterTemplateDN>(EmailMasterTemplateOperation.Save)
            {
                AllowsNew = true,
                Lite = false,
                Execute = (t, _) => t.State = EmailTemplateState.Modified
            }.Register();
        }
    }
}
