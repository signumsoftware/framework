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
using Signum.Engine.SMS;
using Signum.Entities.Processes;
using Signum.Web.Extensions.SMS.Models;
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
            var type = this.ExtractEntity<TypeDN>(prefix);
            return Json(new
            {
                literals = SMSLogic.GetLiteralsFromDataObjectProvider(TypeLogic.ToType(type))
            });
        }

        [HttpPost]
        public PartialViewResult CreateSMSMessageFromTemplate(string prefix)
        {
            var jsFindNavigator = JsFindNavigator.GetFor(prefix);
            ViewData[ViewDataKeys.OnOk] = jsFindNavigator.hasSelectedItem(new JsFunction("item") 
            {
                Js.Submit(Url.Action<SMSController>(sc => sc.CreateMessageFromTemplate()), "{ template: item.key }")
            }).ToJS();

            var ie = this.ExtractEntity<IdentifiableEntity>(null);

            ViewData[ViewDataKeys.Title] = "Select the template";

            return Navigator.PartialFind(this, new FindOptions(typeof(SMSTemplateDN))
            {
                FilterOptions = new List<FilterOption> 
                { 
                    { new FilterOption("IsActive", true) { Frozen = true } },
                    { new FilterOption("AssociatedType", TypeLogic.ToTypeDN(ie.GetType()).ToLite()) }
                }
            }, prefix);
        }

        [HttpPost]
        public ActionResult CreateMessageFromTemplate()
        {
            var ie = this.ExtractLite<IdentifiableEntity>(null);
            var template = Lite.Parse<SMSTemplateDN>(Request["template"]);

            var message = ie.ConstructFromLite<SMSMessageDN>(SMSMessageOperations.CreateSMSMessageFromTemplate, template.Retrieve());
            return Navigator.NormalPage(this, message);
        }

        [HttpPost]
        public PartialViewResult SendMultipleSMSMessagesFromTemplate(List<int> ids, string providerWebQueryName, string prefix)
        {
            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(Navigator.ResolveQueryName(providerWebQueryName));
            Type entitiesType = Lite.Extract(queryDescription.Columns.SingleEx(a => a.IsEntity).Type);
            var webTypeName = Navigator.ResolveWebTypeName(entitiesType);

            prefix = Js.NewPrefix(prefix);
            var jsFindNavigator = JsFindNavigator.GetFor(prefix);
            ViewData[ViewDataKeys.OnOk] = jsFindNavigator.hasSelectedItem(new JsFunction("item") 
            {
                Js.Submit(Url.Action("SendMultipleMessagesFromTemplate", "SMS"),
                    "function() {{ return {{ smsTemplate: item.key, idProviders: '{0}', webTypeName: '{1}' }} }}"
                        .Formato(ids.ToString(","), webTypeName))
            }).ToJS();

            ViewData[ViewDataKeys.Title] = "Select the template";

            return Navigator.PartialFind(this, new FindOptions(typeof(SMSTemplateDN))
            {
                FilterOptions = new List<FilterOption> 
                { 
                    { new FilterOption("IsActive", true) { Frozen = true } },
                    { new FilterOption("AssociatedType", TypeLogic.ToTypeDN(entitiesType).ToLite()) }
                }
            }, prefix);
        }

        [HttpPost]
        public ActionResult SendMultipleMessagesFromTemplate(string idProviders, Lite<SMSTemplateDN> smsTemplate, string webTypeName)
        {
            Type entitiesType = Navigator.ResolveType(webTypeName);

            var process = OperationLogic.ServiceConstructFromMany(
                Database.RetrieveListLite(entitiesType, idProviders.Split(',').Select(id => int.Parse(id)).ToList()),
                entitiesType,
                SMSProviderOperations.SendSMSMessagesFromTemplate,
                smsTemplate.Retrieve());

            return Redirect(Navigator.NavigateRoute(process));
        }

        [HttpPost]
        public PartialViewResult SendMultipleSMSMessages(List<int> ids, string providerWebQueryName, string prefix)
        {
            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(Navigator.ResolveQueryName(providerWebQueryName));
            Type entitiesType = Lite.Extract(queryDescription.Columns.SingleEx(a => a.IsEntity).Type);
            var webTypeName = Navigator.ResolveWebTypeName(entitiesType);

            var model = new MultipleSMSModel
            {
                ProvidersIds = ids.ToString("_"),
                WebTypeName = webTypeName,
            };

            return Navigator.NormalControl(this, model);
        }

        [HttpPost]
        public ActionResult SendMultipleMessages()
        {
            var model = this.ExtractEntity<MultipleSMSModel>(null).ApplyChanges(this.ControllerContext, null, true).Value;

            var cp = new Signum.Engine.SMS.SMSLogic.CreateMessageParams
            {
                Message = model.Message,
                From = model.From
            };

            Type entitiesType = Navigator.ResolveType(model.WebTypeName);

            var process = OperationLogic.ServiceConstructFromMany(
                Database.RetrieveListLite(entitiesType, model.ProvidersIds.Split('_').Select(id => int.Parse(id)).ToList()),
                entitiesType,
                SMSProviderOperations.SendSMSMessage,
                cp);

            return JsonAction.Redirect(Navigator.NavigateRoute(process));
        }
    }
}
