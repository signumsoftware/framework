using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Operations;
using Signum.Entities.SMS;

namespace Signum.Engine.SMS
{
    public class SMSTemplateGraph : Graph<SMSTemplateEntity>
    {
        public static void Register()
        {
            new Construct(SMSTemplateOperation.Create)
            {
                Construct = _ => new SMSTemplateEntity
                {
                    Messages = new Entities.MList<SMSTemplateMessageEntity> 
                    {
                        new SMSTemplateMessageEntity
                        {
                            CultureInfo = SMSLogic.Configuration.DefaultCulture
                        }
                    }
                },
            }.Register();

            new Execute(SMSTemplateOperation.Save)
            {
                Lite = false,
                AllowsNew = true,
                Execute = (t, _) => {}
            }.Register();
        }
    }
}
