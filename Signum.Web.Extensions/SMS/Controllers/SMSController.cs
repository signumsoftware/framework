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
using Signum.Web.Operations;

namespace Signum.Web.SMS
{
    public class SMSController : Controller
    {
        [HttpPost]
        public JsonNetResult GetDictionaries()
        {
            return this.JsonNet(new
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
        public JsonNetResult GetLiteralsForType()
        {
            var type = this.ParseLite<TypeEntity>("type");
            return this.JsonNet(new
            {
                literals = SMSLogic.GetLiteralsFromDataObjectProvider(type.Retrieve().ToType())
            });
        }

        [HttpPost]
        public ActionResult CreateSMSMessageFromTemplate()
        {
            var ie = this.ExtractLite<Entity>();
            var template = Lite.Parse<SMSTemplateEntity>(Request["template"]);

            var message = ie.ConstructFromLite(SMSMessageOperation.CreateSMSWithTemplateFromEntity, template.Retrieve());
            return this.DefaultConstructResult(message);
        }

        [HttpPost]
        public ActionResult SendMultipleMessagesFromTemplate()
        {
            var template = Lite.Parse<SMSTemplateEntity>(Request["template"]);

            var lites = this.ParseLiteKeys<Entity>();

            var process = OperationLogic.ConstructFromMany(lites, SMSMessageOperation.SendSMSMessagesFromTemplate, template.Retrieve());

            return this.DefaultConstructResult(process);
        }

        [HttpPost]
        public PartialViewResult SendMultipleSMSMessagesModel()
        {
            return this.PopupView(new MultipleSMSModel());
        }

        [HttpPost]
        public ActionResult SendMultipleMessages()
        {
            var prefixModel = Request["prefixModel"];

            var model = this.ExtractEntity<MultipleSMSModel>(prefixModel).ApplyChanges(this, prefixModel).Value;

            var lites = this.ParseLiteKeys<Entity>();

            var process = OperationLogic.ConstructFromMany(lites, SMSMessageOperation.SendSMSMessages, model);

            return this.DefaultConstructResult(process);
        }
    }
}
