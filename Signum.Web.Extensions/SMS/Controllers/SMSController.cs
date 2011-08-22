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

            ValueLineBoxModel model = new ValueLineBoxModel(template, ValueLineBoxType.String, Resources.SMS_PhoneNumber, Resources.SMS_WriteTheDestinationNumber);

            ViewData[ViewDataKeys.OnOk] = JsValidator.EntityIsValid(
                new JsValidatorOptions 
                { 
                    Prefix = prefix,
                    ControllerUrl = Url.Action<SMSController>(sc => sc.CreateSMSValidate(prefix))
                },
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
        public ActionResult CreateSMSValidate(string prefix)
        {
            var ctx = this.ExtractEntity<ValueLineBoxModel>(prefix).ApplyChanges(this.ControllerContext, prefix, true).ValidateGlobal();
            
            if (ctx.GlobalErrors.Any())
            {
                ModelState.FromContext(ctx);
                return JsonAction.ModelState(ModelState);
            }
            
            var destinationNumber = ctx.Value.StringValue;
            if (!TelephoneValidatorAttribute.TelephoneRegex.IsMatch(destinationNumber))
            {
                ModelState.AddModelError(prefix, "Telephone is not valid");
                return JsonAction.ModelState(ModelState);
            }

            if (ctx.GlobalErrors.Any())
            {
                ModelState.FromContext(ctx);
            }
            
            return JsonAction.ModelState(ModelState);
        }

        [HttpPost]
        public ActionResult CreateSMSOnOk(string prefix)
        {
            var ctx = this.ExtractEntity<ValueLineBoxModel>(prefix).ApplyChanges(this.ControllerContext, prefix, true);
            var destinationNumber = ctx.Value.StringValue;

            var template = this.ExtractEntity<SMSTemplateDN>("");

            var sms = template.ToLite().ConstructFromLite<SMSMessageDN>(SMSMessageOperations.Create, destinationNumber).Save();

            return Redirect(Navigator.ViewRoute(sms));
        }
    }
}
