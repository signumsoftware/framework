using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Engine.Operations;
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
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using Signum.Engine.DynamicQuery;
using Signum.Web.Basic;
using Signum.Engine.Maps;
using Signum.Web.Cultures;


namespace Signum.Web.SMS
{
    public static class SMSClient
    {
        public static string ViewPrefix = "~/SMS/Views/{0}.cshtml";
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/SMS/Scripts/SMS");

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                CultureInfoClient.Start();

                Navigator.RegisterArea(typeof(SMSClient));
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EmbeddedEntitySettings<SMSConfigurationEntity> { PartialViewName = e => ViewPrefix.FormatWith("SMSConfiguration") },

                    new EntitySettings<SMSMessageEntity> { PartialViewName = e => ViewPrefix.FormatWith("SMSMessage") },
                    new EntitySettings<SMSTemplateEntity> { PartialViewName = e => ViewPrefix.FormatWith("SMSTemplate") },
                    new EmbeddedEntitySettings<SMSTemplateMessageEntity> { PartialViewName = e => ViewPrefix.FormatWith("SMSTemplateMessage") },

                    new EntitySettings<SMSSendPackageEntity> { PartialViewName = e => ViewPrefix.FormatWith("SMSSendPackage") },
                    new EntitySettings<SMSUpdatePackageEntity> { PartialViewName = e => ViewPrefix.FormatWith("SMSUpdatePackage") },

                    new EmbeddedEntitySettings<MultipleSMSModel> { PartialViewName = e => ViewPrefix.FormatWith("MultipleSMS") },
                });

                OperationClient.AddSettings(new List<OperationSettings> 
                {
                    new EntityOperationSettings<Entity>(SMSMessageOperation.CreateSMSWithTemplateFromEntity)
                    {
                        Click = ctx => Module["createSmsWithTemplateFromEntity"](ctx.Options(), JsFunction.Event, 
                            ctx.Url.Action((SMSController sms)=>sms.CreateSMSMessageFromTemplate()), 
                            SmsTemplateFindOptions(ctx.Entity.GetType()).ToJS(ctx.Prefix, "New"))
                    },

                    new ContextualOperationSettings<Entity>(SMSMessageOperation.SendSMSMessagesFromTemplate)
                    {
                        Click  = ctx =>  Module["sendMultipleSMSMessagesFromTemplate"](ctx.Options(), JsFunction.Event, 
                            ctx.Url.Action((SMSController sms )=>sms.SendMultipleMessagesFromTemplate()), 
                            SmsTemplateFindOptions(ctx.SingleType).ToJS(ctx.Prefix, "New"))
                    },

                    new ContextualOperationSettings<Entity>(SMSMessageOperation.SendSMSMessages)
                    {
                        Click  = ctx => Module["sentMultipleSms"](ctx.Options(), JsFunction.Event, ctx.Prefix, 
                            ctx.Url.Action((SMSController sms)=>sms.SendMultipleSMSMessagesModel()),
                            ctx.Url.Action((SMSController sms)=>sms.SendMultipleMessages()))
                    },
                });
            }
        }

        private static FindOptions SmsTemplateFindOptions(Type type)
        {
            return new FindOptions(typeof(SMSTemplateEntity))
            {
                FilterOptions = new List<FilterOption> 
                { 
                    { new FilterOption("IsActive", true) { Frozen = true } },
                    { new FilterOption("AssociatedType", type.ToTypeEntity().ToLite()) }
                }
            };
        }
    }
}
