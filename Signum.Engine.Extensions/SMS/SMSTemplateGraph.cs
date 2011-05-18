using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Operations;
using Signum.Entities.SMS;

namespace Signum.Engine.SMS
{
    public class SMSTemplateGraph : Graph<SMSTemplateDN, SMSTemplateState>
    {
        public static void Register()
        {
            GetState = t => t.State;
            new Construct(SMSTemplateOperations.Create, SMSTemplateState.Created)
            {
                Construct = _ => new SMSTemplateDN { State = SMSTemplateState.Created },
            }.Register();

            new Goto(SMSTemplateOperations.Modify, SMSTemplateState.Modified)
            {
                Lite = false,
                AllowsNew = true,
                FromStates = new[] { SMSTemplateState.Created, SMSTemplateState.Modified },
                Execute = (t, _) => { t.State = SMSTemplateState.Modified; }
            }.Register();

            new Goto(SMSTemplateOperations.Enable, SMSTemplateState.Modified)
            {
                CanExecute = c => c.Active ? "The template is already active" : null,
                FromStates = new[] { SMSTemplateState.Modified },
                Execute = (t, _) => { t.Active = true; }
            }.Register();

            new Goto(SMSTemplateOperations.Disable, SMSTemplateState.Modified)
            {
                CanExecute = c => !c.Active ? "The template is already inactive" : null,
                FromStates = new[] { SMSTemplateState.Modified },
                Execute = (t, _) => { t.Active = false; }
            }.Register();
        }
    }
}
