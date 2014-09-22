using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Reflection;
using System.Reflection;
using Signum.Entities.SMS;
using Signum.Windows.Operations;
using Signum.Entities;
using Signum.Services;

namespace Signum.Windows.SMS
{
    public static class SMSClient
    {
        public static void AsserIsStarted()
        {
            Navigator.Manager.AssertDefined(ReflectionTools.GetMethodInfo(() => SMSClient.Start()));
        }


        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<SMSMessageDN> { View = e => new SMSMessage(), Icon = ExtensionsImageLoader.GetImageSortName("sms.png") },
                    new EntitySettings<SMSTemplateDN> { View = e => new SMSTemplate(), Icon = ExtensionsImageLoader.GetImageSortName("smstemplate.png") },
                    new EmbeddedEntitySettings<SMSTemplateMessageDN>() { View = e => new Signum.Windows.SMS.SMSTemplateMessage() },
                    new EntitySettings<SMSSendPackageDN> { View = e => new SMSSendPackage(), Icon = ExtensionsImageLoader.GetImageSortName("package.png") },
                    new EntitySettings<SMSUpdatePackageDN> { View = e => new SMSUpdatePackage(), Icon = ExtensionsImageLoader.GetImageSortName("package.png") },
                    new EmbeddedEntitySettings<MultipleSMSModel> { View = e => new MultipleSMS(), Icon = ExtensionsImageLoader.GetImageSortName("package.png") },
                });

                OperationClient.AddSetting(new EntityOperationSettings<IdentifiableEntity>(SMSMessageOperation.CreateSMSWithTemplateFromEntity)
                {
                    Click = FindAssociatedTemplates
                });

                OperationClient.AddSetting(new ContextualOperationSettings<IdentifiableEntity>(SMSMessageOperation.SendSMSMessages)
                {
                    Click = coc =>
                    {
                        MultipleSMSModel model = Navigator.View(new MultipleSMSModel());

                        if (model == null)
                            return;

                        var result = new ConstructorContext(coc.SearchControl, coc.OperationInfo).SurroundConstruct(ctx =>
                            Server.Return((IOperationServer s) => s.ConstructFromMany(coc.Entities, coc.Type, coc.OperationInfo.OperationSymbol, ctx.Args)));

                        if (result != null)
                            Navigator.Navigate(result);
                    }
                });
            }
        }

        public static IdentifiableEntity FindAssociatedTemplates(EntityOperationContext<IdentifiableEntity> e)
        {
            var template = Finder.Find(new FindOptions(typeof(SMSTemplateDN))
            {
                FilterOptions = new List<FilterOption>
                {
                    { new FilterOption("IsActive", true) { Frozen = true } },
                    { new FilterOption("AssociatedType", Server.ServerTypes[e.Entity.GetType()]) }
                },
                SearchOnLoad = true,
            });

            if (template != null)
                Navigator.Navigate(e.Entity.ToLite().ConstructFromLite(SMSMessageOperation.CreateSMSWithTemplateFromEntity, ((Lite<SMSTemplateDN>)template).Retrieve()));

            return null;
        }
    }
}
