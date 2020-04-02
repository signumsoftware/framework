using Signum.React.Json;
using Signum.Utilities;
using System.Reflection;
using Signum.Engine.Basics;
using Signum.React.ApiControllers;
using Signum.React.Facades;
using Signum.Engine.Maps;
using Signum.Entities.Mailing;
using Signum.Entities.Templating;
using Signum.Engine.Mailing;
using Signum.React.TypeHelp;
using Microsoft.AspNetCore.Builder;
using Signum.Utilities.Reflection;
using Signum.Engine.Mailing.Pop3;

namespace Signum.React.Mailing
{
    public static class MailingServer
    {
        public static void Start(IApplicationBuilder app, bool pop3, bool emailSender, bool smtp)
        {
            TypeHelpServer.Start(app);
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            ReflectionServer.RegisterLike(typeof(TemplateTokenMessage));

            EntityJsonConverter.AfterDeserilization.Register((EmailTemplateEntity et) =>
            {
                if (et.Query != null)
                {
                    var qd = QueryLogic.Queries.QueryDescription(et.Query.ToQueryName());
                    et.ParseData(qd);
                }
            });

            QueryDescriptionTS.AddExtension += qd =>
            {
                object type = QueryLogic.ToQueryName(qd.queryKey);
                if (Schema.Current.IsAllowed(typeof(EmailTemplateEntity), true) == null)
                {
                    var templates = EmailTemplateLogic.GetApplicableEmailTemplates(type, null, EmailTemplateVisibleOn.Query);

                    if (templates.HasItems())
                        qd.Extension.Add("emailTemplates", templates);
                }
            };

            if (pop3)
            {
                var piPassword = ReflectionTools.GetPropertyInfo((Pop3ConfigurationEntity e) => e.Password);
                var pcs = PropertyConverter.GetPropertyConverters(typeof(Pop3ConfigurationEntity));
                pcs.GetOrThrow("password").CustomWriteJsonProperty = ctx => { };
                pcs.Add("newPassword", new PropertyConverter
                {
                    AvoidValidate = true,
                    CustomWriteJsonProperty = ctx => { },
                    CustomReadJsonProperty = ctx =>
                    {
                        EntityJsonConverter.AssertCanWrite(ctx.ParentPropertyRoute.Add(piPassword));

                        var password = (string)ctx.JsonReader.Value!;

                        ((Pop3ConfigurationEntity)ctx.Entity).Password = Pop3ConfigurationLogic.EncryptPassword(password);
                    }
                });
            }

            if (emailSender)
            {
                if (smtp)
                {
                    var piPassword = ReflectionTools.GetPropertyInfo((SmtpNetworkDeliveryEmbedded e) => e.Password);
                    var pcs = PropertyConverter.GetPropertyConverters(typeof(SmtpNetworkDeliveryEmbedded));
                    pcs.GetOrThrow("password").CustomWriteJsonProperty = ctx => { };
                    pcs.Add("newPassword", new PropertyConverter
                    {
                        AvoidValidate = true,
                        CustomWriteJsonProperty = ctx => { },
                        CustomReadJsonProperty = ctx =>
                        {
                            EntityJsonConverter.AssertCanWrite(ctx.ParentPropertyRoute.Add(piPassword));

                            var password = (string)ctx.JsonReader.Value!;

                            ((SmtpNetworkDeliveryEmbedded)ctx.Entity).Password = EmailSenderConfigurationLogic.EncryptPassword(password);
                        }
                    });
                }
            }
        }
    }
}
