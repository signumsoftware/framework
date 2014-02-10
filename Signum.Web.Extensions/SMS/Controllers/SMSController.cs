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
using Signum.Engine.SMS;
using Signum.Entities.Processes;
using Signum.Web.Extensions.SMS.Models;
using Signum.Web.Operations;
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
        public ContentResult RemoveNoSMSCharacters(string text)
        {
            return Content(SMSCharacters.RemoveNoSMSCharacters(text));
        }

        [HttpPost]
        public JsonResult GetLiteralsForType(string prefix)
        {
            var type = this.ExtractEntity<TypeDN>();
            return Json(new
            {
                literals = SMSLogic.GetLiteralsFromDataObjectProvider(type.ToType())
            });
        }

        [HttpPost]
        public ActionResult CreateSMSMessageFromTemplate()
        {
            var ie = this.ExtractLite<IdentifiableEntity>();
            var template = Lite.Parse<SMSTemplateDN>(Request["template"]);

            var message = ie.ConstructFromLite<SMSMessageDN>(SMSMessageOperation.CreateSMSWithTemplateFromEntity, template.Retrieve());
            return OperationClient.DefaultConstructResult(this, message);
        }

        [HttpPost]
        public ActionResult SendMultipleMessagesFromTemplate()
        {
            var template = Lite.Parse<SMSTemplateDN>(Request["template"]);

            var lites = this.ParseLiteKeys<IdentifiableEntity>();

            var process = OperationLogic.ServiceConstructFromMany(lites, lites.First().EntityType, SMSProviderOperation.SendSMSMessagesFromTemplate, template.Retrieve());

            return OperationClient.DefaultConstructResult(this, process);
        }

        [HttpPost]
        public PartialViewResult SendMultipleSMSMessagesModel()
        {
            var model = new MultipleSMSModel
            {
            };

            return this.PopupOpen(new PopupViewOptions(new TypeContext<MultipleSMSModel>(model, Request["newPrefix"])));
        }

        [HttpPost]
        public ActionResult SendMultipleMessages()
        {
            var prefixModel = Request["prefixModel"];

            var model = this.ExtractEntity<MultipleSMSModel>(prefixModel).ApplyChanges(this.ControllerContext, true, prefixModel).Value;

            var lites = this.ParseLiteKeys<IdentifiableEntity>();

            var cp = new Signum.Engine.SMS.SMSLogic.CreateMessageParams
            {
                Message = model.Message,
                From = model.From,
            };

            var process = OperationLogic.ServiceConstructFromMany(lites, lites.First().EntityType, SMSProviderOperation.SendSMSMessage, cp);

            return OperationClient.DefaultConstructResult(this, process);
        }
    }
}
