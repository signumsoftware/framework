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
            ViewData[ViewDataKeys.OnOk] = new JsFindNavigator(prefix).hasSelectedItems(
                  new JsFunction() 
                {
                       Js.Submit(RouteHelper.New().Action("CreateMessageFromTemplate", "SMS"),
                        "function() {{ return {{ smsTemplateID: {0} }} }}".Formato(new JsFindNavigator(prefix).splitSelectedIds().ToJS()))
                }).ToJS();

            var ie = this.ExtractEntity<IdentifiableEntity>(null);

            ViewData[ViewDataKeys.Title] = "Select the template";

            return Navigator.PartialFind(this, new FindOptions(typeof(SMSTemplateDN))
            {
                FilterOptions = new List<FilterOption> 
                { 
                    { new FilterOption("IsActive", true) { Frozen = true } },
                    { new FilterOption("AssociatedType", TypeLogic.ToTypeDN(ie.GetType()).ToLite()) { Frozen = true } }
                },
                AllowMultiple = false
            }, prefix);
        }

        [HttpPost]
        public ActionResult CreateMessageFromTemplate(string smsTemplateID)
        {
            var ie = this.ExtractLite<IdentifiableEntity>(null);
            var template = Database.Retrieve<SMSTemplateDN>(int.Parse(smsTemplateID));
            var message = ie.ConstructFromLite<SMSMessageDN>(SMSMessageOperations.CreateSMSMessageFromTemplate, template);
            return Navigator.View(this, message);
        }

        [HttpPost]
        public PartialViewResult SendMultipleSMSMessageFromTemplate(List<int> ids, string providerWebQueryName, string prefix)
        {
            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(Navigator.ResolveQueryName(providerWebQueryName));
            Type entitiesType = Reflector.ExtractLite(queryDescription.Columns.Single(a => a.IsEntity).Type);
            var webTypeName = Navigator.ResolveWebTypeName(entitiesType);

            //TODO: Anto ConstructorFromMany no pasa prefijo nuevo
            prefix = Js.NewPrefix(prefix);
            ViewData[ViewDataKeys.OnOk] = new JsFindNavigator(prefix).hasSelectedItems(
                  new JsFunction() 
                {
                       Js.Submit(RouteHelper.New().Action("SendMultipleMessageFromTemplate", "SMS"),
                        "function() {{ return {{ smsTemplateID: {0}, idProviders: '{1}', webTypeName: '{2}' }} }}"
                        .Formato(new JsFindNavigator(prefix).splitSelectedIds().ToJS(), ids.ToString(","), webTypeName))
                }).ToJS();

            ViewData[ViewDataKeys.Title] = "Select the template";

            return Navigator.PartialFind(this, new FindOptions(typeof(SMSTemplateDN))
            {
                FilterOptions = new List<FilterOption> 
                { 
                    { new FilterOption("IsActive", true) { Frozen = true } },
                    { new FilterOption("AssociatedType", TypeLogic.ToTypeDN(entitiesType).ToLite()) { Frozen = true } }
                },
                AllowMultiple = false
            }, prefix);
        }

        [HttpPost]
        public ActionResult SendMultipleMessageFromTemplate(string idProviders, string smsTemplateID, string webTypeName)
        {
            Type entitiesType = Navigator.ResolveType(webTypeName);

            var process = OperationLogic.ServiceConstructFromMany(
                Database.RetrieveListLite(entitiesType, idProviders.Split(',').Select(id => int.Parse(id)).ToList()), 
                entitiesType, //typeof(ProcessExecutionDN), 
                SMSProviderOperations.SendSMSMessagesFromTemplate, 
                Database.Retrieve<SMSTemplateDN>(int.Parse(smsTemplateID)));

            return Navigator.View(this, process);
        }
    }
}
