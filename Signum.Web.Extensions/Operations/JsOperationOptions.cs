using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;

namespace Signum.Web.Operations
{
    public class JsOperationOptions : JsRenderer
    {
        public JsValue<string> Prefix { get; set; }
        public JsValue<string> OperationKey { get; set; }
        public JsValue<bool?> IsLite { get; set; }
        public JsValue<string> ReturnType { get; set; }
        public JsValue<string> ControllerUrl { get; set; }
        public JsInstruction RequestExtraJsonData { get; set; }
        //public string ValidationControllerUrl { get; set; }
        //public bool AvoidValidation { get; set; }
        //public bool AvoidDefaultOk { get; set; }
        //public string OnOk { get; set; }
        //public string OnOperationSuccess { get; set; }
        //public string OnCancelled { get; set; }
        //public bool MultiStep { get; set; }
        //public bool NavigateOnSuccess { get; set; }
        //public bool ClosePopupOnSuccess { get; set; }
        //public string ConfirmMessage { get; set; }
     
        //public bool Post { get; set; }

        public JsOperationOptions()
        {
            Renderer = () =>
            {
                //if (AvoidDefaultOk && OnOk.HasText())
                //    throw new ArgumentException("JsOperationOptions cannot have both AvoidDefaultOk and OnOk specified");

                var builder = new JsOptionsBuilder(false)
                {
                    {"prefix", Prefix.TryCC(a=>a.ToJS())},
                    {"operationKey", OperationKey.TryCC(a=>a.ToJS())},
                    {"isLite", IsLite.TryCC(a=>a.ToJS())},
                    {"returnType",ReturnType.TryCC(a=>a.ToJS())},
                    {"requestExtraJsonData", RequestExtraJsonData.TryCC(a=>a.ToJS())},
                    {"controllerUrl", ControllerUrl.TryCC(a=>a.ToJS())}
                    //{"validationControllerUrl", ValidationControllerUrl.TrySingleQuote()},
                    //{"avoidValidation", AvoidValidation == true? "true": null},
                    //{"onOk", OnOk ?? (AvoidDefaultOk ? JsOperationBase.AvoidDefaultOk(null).ToJS() : null)},
                    //{"onOperationSuccess", OnOperationSuccess },
                   // {"onCancelled", OnCancelled},
                    //{"multiStep", MultiStep == true? "true": null},
                    //{"post", Post == true? "true": null},
                    //{"navigateOnSuccess", NavigateOnSuccess == true? "true": null},
                    //{"closePopupOnSuccess", ClosePopupOnSuccess== true? "true": null},
                    //{"confirmMsg", ConfirmMessage.TrySingleQuote() },
                    
                };

                //if (ControllerUrl != null && ControllerUrl.ToJS() != "null")
                //    builder.Add("controllerUrl", ControllerUrl.ToJS());
                
                return builder.ToJS();
            };
        }
    }
}
