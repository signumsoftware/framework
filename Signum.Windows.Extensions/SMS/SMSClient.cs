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
                    new EntitySettings<SMSMessageEntity> { View = e => new SMSMessage(), Icon = ExtensionsImageLoader.GetImageSortName("sms.png") },
                    new EntitySettings<SMSTemplateEntity> { View = e => new SMSTemplate(), Icon = ExtensionsImageLoader.GetImageSortName("smstemplate.png") },
                    new EmbeddedEntitySettings<SMSTemplateMessageEntity>() { View = e => new Signum.Windows.SMS.SMSTemplateMessage() },
                    new EntitySettings<SMSSendPackageEntity> { View = e => new SMSSendPackage(), Icon = ExtensionsImageLoader.GetImageSortName("package.png") },
                    new EntitySettings<SMSUpdatePackageEntity> { View = e => new SMSUpdatePackage(), Icon = ExtensionsImageLoader.GetImageSortName("package.png") },
                    new EmbeddedEntitySettings<MultipleSMSModel> { View = e => new MultipleSMS(), Icon = ExtensionsImageLoader.GetImageSortName("package.png") },
                });

                OperationClient.AddSetting(new EntityOperationSettings<Entity>(SMSMessageOperation.CreateSMSWithTemplateFromEntity)
                {
                    Click = FindAssociatedTemplates
                });

                OperationClient.AddSetting(new ContextualOperationSettings<Entity>(SMSMessageOperation.SendSMSMessages)
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

        public static Entity FindAssociatedTemplates(EntityOperationContext<Entity> e)
        {
            var template = Finder.Find(new FindOptions(typeof(SMSTemplateEntity))
            {
                FilterOptions = new List<FilterOption>
                {
                    { new FilterOption("IsActive", true) { Frozen = true } },
                    { new FilterOption("AssociatedType", Server.ServerTypes[e.Entity.GetType()]) }
                },
                SearchOnLoad = true,
            });

            if (template != null)
                Navigator.Navigate(e.Entity.ToLite().ConstructFromLite(SMSMessageOperation.CreateSMSWithTemplateFromEntity, ((Lite<SMSTemplateEntity>)template).Retrieve()));

            return null;
        }
    }
}
