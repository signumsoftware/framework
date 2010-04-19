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
        public string Prefix { get; set; }
        public string OperationKey { get; set; }
        public bool? IsLite { get; set; }
        public Type ReturnType { get; set; }
        public string ControllerUrl { get; set; }
        public string ValidationControllerUrl { get; set; }
        public bool AvoidValidation { get; set; }
        public bool AvoidDefaultOk { get; set; }
        public string OnOk { get; set; }
        public string OnOperationSuccess { get; set; }
        public string OnCancelled { get; set; }
        public bool MultiStep { get; set; }
        public bool NavigateOnSuccess { get; set; }
        public bool ClosePopupOnSuccess { get; set; }
        public string ConfirmMessage { get; set; }
        public string RequestExtraJsonData { get; set; }

        public JsOperationOptions()
        {
            renderer = () =>
            {
                if (AvoidDefaultOk && OnOk.HasText())
                    throw new ArgumentException("JsOperationOptions cannot have both AvoidDefaultOk and OnOk specified");

                return new JsOptionsBuilder(false)
                {
                    {"prefix", Prefix.TrySingleQuote()},
                    {"operationKey", OperationKey.TrySingleQuote()},
                    {"isLite", IsLite == true? "true": null},
                    {"returnType",ReturnType.TryCC(a=>a.Name).TrySingleQuote()},
                    {"controllerUrl", ControllerUrl.TrySingleQuote()},
                    {"validationControllerUrl", ValidationControllerUrl.TrySingleQuote()},
                    {"avoidValidation", AvoidValidation == true? "true": null},
                    {"onOk", OnOk ?? (AvoidDefaultOk ? JsOperationBase.AvoidDefaultOk(null).ToJS() : null)},
                    {"onOperationSuccess", OnOperationSuccess },
                    {"onCancelled", OnCancelled},
                    {"multiStep", MultiStep == true? "true": null},
                    {"navigateOnSuccess", NavigateOnSuccess == true? "true": null},
                    {"closePopupOnSuccess", ClosePopupOnSuccess== true? "true": null},
                    {"confirmMsg", ConfirmMessage.TrySingleQuote() },
                    {"requestExtraJsonData", RequestExtraJsonData},
                }.ToJS();
            };
        }
    }
}
