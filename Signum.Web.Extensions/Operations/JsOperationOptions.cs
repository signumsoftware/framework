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
        public bool? AvoidValidation { get; set; }
        public bool AvoidDefaultOk { get; set; }
        public string OnOk { get; set; }
        public string OnOperationSuccess { get; set; }
        public string OnCancelled { get; set; }
        public bool? MultiStep { get; set; }
        public bool? NavigateOnSuccess { get; set; }
        public bool? ClosePopupOnSuccess { get; set; }
        public string ConfirmMessage { get; set; }
        public string RequestExtraJsonData { get; set; }
        
        public JsOperationOptions()
        {
            renderer = () =>
            {
                StringBuilder sb = new StringBuilder();

                if (Prefix.HasText())
                    sb.Append("prefix:'{0}',".Formato(Prefix));

                if (OperationKey.HasText())
                    sb.Append("operationKey:'{0}',".Formato(OperationKey));

                if (IsLite == true)
                    sb.Append("isLite:'{0}',".Formato(IsLite.Value));

                if (ReturnType != null)
                    sb.Append("returnType:'{0}',".Formato(ReturnType.Name));
                
                if (ControllerUrl.HasText())
                    sb.Append("controllerUrl:'{0}',".Formato(ControllerUrl));
                
                if (ValidationControllerUrl.HasText())
                    sb.Append("validationControllerUrl:'{0}',".Formato(ValidationControllerUrl));

                if (AvoidValidation == true)
                    sb.Append("avoidValidation:'{0}',".Formato(AvoidValidation.Value));

                if (AvoidDefaultOk && OnOk.HasText())
                    throw new ArgumentException("JsOperationOptions cannot have both AvoidDefaultOk and OnOk specified");

                if (OnOk.HasText())
                    sb.Append("onOk:{0},".Formato(OnOk));

                if (AvoidDefaultOk)
                    sb.Append("onOk:{0},".Formato(JsOperationBase.AvoidDefaultOk(null).ToJS()));

                if (OnOperationSuccess.HasText())
                    sb.Append("onOperationSuccess:{0},".Formato(OnOperationSuccess));

                if (OnCancelled.HasText())
                    sb.Append("onCancelled:{0},".Formato(OnCancelled));

                if (MultiStep == true)
                    sb.Append("multiStep:'{0}',".Formato(MultiStep.Value));

                if (NavigateOnSuccess == true)
                    sb.Append("navigateOnSuccess:'{0}',".Formato(NavigateOnSuccess.Value));

                if (ClosePopupOnSuccess == true)
                    sb.Append("closePopupOnSuccess:'{0}',".Formato(ClosePopupOnSuccess.Value));

                if (ConfirmMessage.HasText())
                    sb.Append("confirmMsg:'{0}',".Formato(ConfirmMessage));

                if (RequestExtraJsonData.HasText())
                    sb.Append("requestExtraJsonData:{0},".Formato(RequestExtraJsonData));

                string result = sb.ToString();

                return result.HasText() ? 
                    "{" + result.Substring(0, result.Length - 1) + "}" :
                    null; //Instead of "" so we can use Combine string extension
            };
        }
    }
}
