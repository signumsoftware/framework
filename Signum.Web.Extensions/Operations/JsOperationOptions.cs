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
        //public string Type { get; set; }
        //public int? Id { get; set; }
        public Type ReturnType { get; set; }
        public string ControllerUrl { get; set; }
        public string OnOk { get; set; }
        public bool? MultiStep { get; set; }
        public bool? NavigateOnSuccess { get; set; }
        public string ConfirmMessage { get; set; }

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
                //if (Type.HasText())
                //    sb.Append("type:'{0}',".Formato(Type));

                //if (Id.HasValue)
                //    sb.Append("id:'{0}',".Formato(Id.Value.ToString()));

                if (ControllerUrl.HasText())
                    sb.Append("controllerUrl:'{0}',".Formato(ControllerUrl));

                if (OnOk.HasText())
                    sb.Append("onOk:{0},".Formato(OnOk));

                if (MultiStep == true)
                    sb.Append("multiStep:'{0}',".Formato(MultiStep.Value));

                if (NavigateOnSuccess == true)
                    sb.Append("navigateOnSuccess:'{0}',".Formato(NavigateOnSuccess.Value));

                if (ConfirmMessage.HasText())
                    sb.Append("confirmMsg:'{0}',".Formato(ConfirmMessage));

                string result = sb.ToString();

                return result.HasText() ? 
                    "{" + result.Substring(0, result.Length - 1) + "}" :
                    null; //Instead of "" so we can use Combine string extension
            };
        }
    }
}
