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
        public SMSTemplateGraph()
        {
            this.GetState = t => t.State;
            this.Operations = new List<IGraphOperation> 
            { 
                new Construct(SMSTemplateOperations.Create, SMSTemplateState.Disabled)
                {
                    Constructor = _ => new SMSTemplateDN{},
                },

                new Goto(SMSTemplateOperations.Modify, SMSTemplateState.Disabled)
                {
                    Lite = false,
                    AllowsNew = true,
                    FromStates = new [] { SMSTemplateState.Disabled },
                    Execute = (t, _) => { t.State = SMSTemplateState.Disabled; }
                },

                new Goto(SMSTemplateOperations.Enable, SMSTemplateState.Enabled)
                {
                    FromStates = new [] { SMSTemplateState.Disabled },
                    Execute = (t, _) => { t.State = SMSTemplateState.Enabled; }
                },

                new Goto(SMSTemplateOperations.Disable, SMSTemplateState.Disabled)
                {
                    FromStates = new [] { SMSTemplateState.Enabled },
                    Execute = (t, _) => { t.State = SMSTemplateState.Disabled; }                    
                }
            };
        }
    }
}
