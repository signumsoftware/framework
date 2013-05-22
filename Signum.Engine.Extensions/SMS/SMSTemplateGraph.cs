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
            new Construct(SMSTemplateOperation.Create)
            {
                ToState = SMSTemplateState.Created,
                Construct = _ => new SMSTemplateDN { State = SMSTemplateState.Created },
            }.Register();

            new Execute(SMSTemplateOperation.Save)
            {
                Lite = false,
                AllowsNew = true,
                FromStates = { SMSTemplateState.Created, SMSTemplateState.Modified },
                ToState = SMSTemplateState.Modified,
                Execute = (t, _) => { t.State = SMSTemplateState.Modified; }
            }.Register();

            new Execute(SMSTemplateOperation.Enable)
            {
                FromStates = { SMSTemplateState.Modified },
                ToState = SMSTemplateState.Modified,
                CanExecute = c => c.Active ? "The template is already active" : null,                
                Execute = (t, _) => { t.Active = true; }
            }.Register();

            new Execute(SMSTemplateOperation.Disable)
            {
                CanExecute = c => !c.Active ? "The template is already inactive" : null,
                FromStates = { SMSTemplateState.Modified },
                ToState = SMSTemplateState.Modified,
                Execute = (t, _) => { t.Active = false; }
            }.Register();

            registered = true;
        }
    }
}
