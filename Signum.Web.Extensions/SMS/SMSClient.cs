#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Engine.Operations;
using Signum.Entities.Operations;
using Signum.Utilities;
using Signum.Entities;
using System.Web;
using Signum.Entities.Basics;
using System.Reflection;
using System.Web.UI;
using System.IO;
using System.Web.Routing;
using Signum.Entities.SMS;
using Signum.Web.Operations;
#endregion


namespace Signum.Web.SMS
{
    public static class SMSClient
    {
        public static string ViewPrefix = "~/SMS/Views/{0}.cshtml";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(SMSClient));
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<SMSMessageDN>(EntityType.NotSaving){ PartialViewName = e => ViewPrefix.Formato("SMSMessage")},
                    new EntitySettings<SMSTemplateDN>(EntityType.NotSaving){ PartialViewName = e => ViewPrefix.Formato("SMSTemplate")},
                });

                OperationsClient.Manager.Settings.AddRange(new Dictionary<Enum, OperationSettings> 
                {
                    {SMSMessageOperations.CreateSMSMessageFromTemplate, new EntityOperationSettings
                    {
                        OnClick = ctx => new JsOperationExecutor(ctx.Options("CreateSMSMessageFromTemplate", "SMS"))
                        .ajax(Js.NewPrefix(ctx.Prefix), JsOpSuccess.OpenPopupNoDefaultOk)
                    }},

                    {SMSProviderOperations.SendSMSMessagesFromTemplate, new QueryOperationSettings
                    {
                        RequestExtraJsonData = "function(){ return { providerWebQueryName: $('#sfWebQueryName').val() }; }",
                        OnClick = ctx => new JsFindNavigator(ctx.Prefix).hasSelectedItems(
                            new JsFunction("items")
                            {
                                new JsOperationConstructorFromMany(ctx.Options("SendMultipleSMSMessageFromTemplate","SMS")).ajaxSelected(
                                    Js.NewPrefix(ctx.Prefix),
                                    new JsValue<string>("items").ToJS(),
                                    JsOpSuccess.OpenPopupNoDefaultOk)
                            }),
                        }
                    },
                });
            }
        }
    }
}
