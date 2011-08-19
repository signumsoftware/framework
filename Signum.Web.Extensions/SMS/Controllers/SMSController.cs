#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Engine;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Web.Extensions.Properties;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.Entities.SMS;
using Signum.Engine.Operations;
#endregion

namespace Signum.Web.SMS
{
    public class SMSController : Controller
    {
        [HttpPost]
        public JsonResult GetDictionaries()
        {
            return Json(new
                {
                    smsLength = SMSCharacters.SMSMaxTextLength,
                    smsWarningLength = SMSCharacters.SMSMaxTextLength / 4,
                    normalChar = SMSCharacters.NormalCharacters.Select(li => li.Key).ToList(),
                    doubleChar = SMSCharacters.DoubleCharacters.Select(li => li.Key).ToList()
                });
        }

        [HttpPost]
        public ActionResult CreateSMS(string prefix)
        {
            var template = this.ExtractEntity<SMSTemplateDN>("");

            ValueLineBoxModel model = new ValueLineBoxModel(template, ValueLineBoxType.String, Resources.SMS_WriteTheDestinationNumber, Resources.SMS_PhoneNumber);

            ViewData[ViewDataKeys.OnOk] = JsValidator.EntityIsValid(prefix,
                new JsFunction() 
                {
                    Js.Submit(Url.Action<SMSController>(sc => sc.CreateSMSOnOk(prefix)),
                      "function() {{ return SF.Popup.serializeJson('{0}'); }}".Formato(prefix))
                }).ToJS();

            ViewData[ViewDataKeys.Title] = Resources.SMS_DestinationNumber;
            ViewData[ViewDataKeys.WriteSFInfo] = true;

            var tc = new TypeContext<ValueLineBoxModel>(model, prefix);
            return this.PopupOpen(new ViewOkOptions(tc));
        }

        [HttpPost]
        public ActionResult CreateSMSOnOk(string prefix)
        {
            var template = this.ExtractEntity<SMSTemplateDN>("");
            var destinationNumber = this.ExtractEntity<ValueLineBoxModel>(prefix).ApplyChanges(this.ControllerContext, prefix, true).Value.StringValue;

            var sms = template.ToLite().ConstructFromLite<SMSMessageDN>(SMSMessageOperations.Create, destinationNumber).Save();

            return Redirect(Navigator.ViewRoute(sms));
        }
    }
}
