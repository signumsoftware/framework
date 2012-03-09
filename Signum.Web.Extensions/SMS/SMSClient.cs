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
using Signum.Web.Extensions.SMS.Models;
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
                    new EntitySettings<SMSMessageDN>(EntityType.AdminNotSaving){ PartialViewName = e => ViewPrefix.Formato("SMSMessage")},
                    new EntitySettings<SMSTemplateDN>(EntityType.AdminNotSaving){ PartialViewName = e => ViewPrefix.Formato("SMSTemplate")},
                    new EntitySettings<SMSSendPackageDN>(EntityType.ServerOnly){ PartialViewName = e => ViewPrefix.Formato("SMSSendPackage") },
                    new EntitySettings<SMSUpdatePackageDN>(EntityType.ServerOnly){ PartialViewName = e => ViewPrefix.Formato("SMSUpdatePackage") },

                    new EmbeddedEntitySettings<MultipleSMSModel>() { PartialViewName = e => ViewPrefix.Formato("MultipleSMS"), ShowSave = false},
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
                        OnClick = ctx => new JsOperationConstructorFromMany(ctx.Options("SendMultipleSMSMessagesFromTemplate","SMS"))
                                .ajaxSelected(Js.NewPrefix(ctx.Prefix), JsOpSuccess.OpenPopupNoDefaultOk),
                    }},

                    {SMSProviderOperations.SendSMSMessage, new QueryOperationSettings
                    {
                        RequestExtraJsonData = "function(){ return { providerWebQueryName: $('#sfWebQueryName').val() }; }",
                        OnClick = ctx => new JsOperationConstructorFromMany(ctx.Options("SendMultipleSMSMessages","SMS"))
                                .ajaxSelected(ctx.Prefix, JsOpSuccess.DefaultDispatcher),
                    }},
                });

                ButtonBarEntityHelper.RegisterEntityButtons<MultipleSMSModel>((ctx, entity) => 
                {
                    return new ToolBarButton[]
                    {
                        new ToolBarButton 
                        { 
                            Id = "Send", 
                            Text = "Send Message", 
                            DivCssClass = ToolBarButton.DefaultEntityDivCssClass,
                            OnClick = new JsOperationExecutor(new JsOperationOptions
                            {
                                ControllerUrl = RouteHelper.New().Action<SMSController>(cu => cu.SendMultipleMessages())
                            }).validateAndAjax().ToJS()
                        }
                    };
                });
            }
        }
    }
}
