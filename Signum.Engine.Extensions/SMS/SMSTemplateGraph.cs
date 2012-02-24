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
        static bool registered;
        public static bool Registered { get { return registered; } }


        public static void Register()
        {
            GetState = t => t.State;
            new Construct(SMSTemplateOperations.Create)
            {
                ToState = SMSTemplateState.Created,
                Construct = _ => new SMSTemplateDN { State = SMSTemplateState.Created },
            }.Register();

            new Execute(SMSTemplateOperations.Save)
            {
                Lite = false,
                AllowsNew = true,
                FromStates = new[] { SMSTemplateState.Created, SMSTemplateState.Modified },
                ToState = SMSTemplateState.Modified,
                Execute = (t, _) => { t.State = SMSTemplateState.Modified; }
            }.Register();

            new Execute(SMSTemplateOperations.Enable)
            {
                FromStates = new[] { SMSTemplateState.Modified },
                ToState = SMSTemplateState.Modified,
                CanExecute = c => c.Active ? "The template is already active" : null,                
                Execute = (t, _) => { t.Active = true; }
            }.Register();

            new Execute(SMSTemplateOperations.Disable)
            {
                CanExecute = c => !c.Active ? "The template is already inactive" : null,
                FromStates = new[] { SMSTemplateState.Modified },
                ToState = SMSTemplateState.Modified,
                Execute = (t, _) => { t.Active = false; }
            }.Register();

            registered = true;
        }
    }
}
