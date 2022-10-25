using Signum.React.ApiControllers;
using Signum.React.Facades;
using Signum.Entities.Mailing;
using Signum.Engine.Mailing;
using Signum.React.TypeHelp;
using Microsoft.AspNetCore.Builder;
using Signum.Engine.Authorization;
using Signum.React.Extensions.Templating;
using Microsoft.Exchange.WebServices.Data;
using Signum.Entities.Authorization;
using System.Net.Mail;
using Signum.Utilities.Reflection;
using Signum.Engine.Mailing.Pop3;
using System.Text.Json;
using Signum.Engine.Json;

namespace Signum.React.Mailing;

public static class MailingServer
{
    public static void Start(IApplicationBuilder app)
    {
        TypeHelpServer.Start(app);
        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
        ReflectionServer.OverrideIsNamespaceAllowed.Add(typeof(ExchangeVersion).Namespace!, () => TypeAuthLogic.GetAllowed(typeof(EmailSenderConfigurationEntity)).MaxUI() > TypeAllowedBasic.None);
        ReflectionServer.OverrideIsNamespaceAllowed.Add(typeof(SmtpDeliveryMethod).Namespace!, () => TypeAuthLogic.GetAllowed(typeof(EmailSenderConfigurationEntity)).MaxUI() > TypeAllowedBasic.None);


        TemplatingServer.Start(app);

        SignumServer.WebEntityJsonConverterFactory.AfterDeserilization.Register((EmailTemplateEntity et) =>
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


        if (Schema.Current.Tables.ContainsKey(typeof(SmtpEmailServiceEntity)))
        {
            var piPassword = ReflectionTools.GetPropertyInfo((SmtpNetworkDeliveryEmbedded e) => e.Password);
            var pcs = SignumServer.WebEntityJsonConverterFactory.GetPropertyConverters(typeof(SmtpNetworkDeliveryEmbedded));
            pcs.GetOrThrow("password").CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) => { };
            pcs.Add("newPassword", new PropertyConverter
            {
                AvoidValidate = true,
                CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) => { },
                CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
                {
                    ctx.Factory.AssertCanWrite(ctx.ParentPropertyRoute.Add(piPassword), ctx.Entity);

                    var password = reader.GetString()!;

                    ((SmtpNetworkDeliveryEmbedded)ctx.Entity).Password = EmailSenderConfigurationLogic.EncryptPassword(password);
                }
            });
        }

        if (Schema.Current.Tables.ContainsKey(typeof(ExchangeWebServiceEmailServiceEntity)))
        {
            var piPassword = ReflectionTools.GetPropertyInfo((ExchangeWebServiceEmailServiceEntity e) => e.Password);
            var pcs = SignumServer.WebEntityJsonConverterFactory.GetPropertyConverters(typeof(ExchangeWebServiceEmailServiceEntity));
            pcs.GetOrThrow("password").CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) => { };
            pcs.Add("newPassword", new PropertyConverter
            {
                AvoidValidate = true,
                CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) => { },
                CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
                {
                    ctx.Factory.AssertCanWrite(ctx.ParentPropertyRoute.Add(piPassword), ctx.Entity);

                    var password = reader.GetString()!;

                    ((ExchangeWebServiceEmailServiceEntity)ctx.Entity).Password = EmailSenderConfigurationLogic.EncryptPassword(password);
                }
            });
        }

        if (Schema.Current.Tables.ContainsKey(typeof(Pop3ConfigurationEntity)))
        {
            var piPassword = ReflectionTools.GetPropertyInfo((Pop3ConfigurationEntity e) => e.Password);
            var pcs = SignumServer.WebEntityJsonConverterFactory.GetPropertyConverters(typeof(Pop3ConfigurationEntity));
            pcs.GetOrThrow("password").CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) => { };
            pcs.Add("newPassword", new PropertyConverter
            {
                AvoidValidate = true,
                CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) => { },
                CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
                {
                    ctx.Factory.AssertCanWrite(ctx.ParentPropertyRoute.Add(piPassword), ctx.Entity);

                    var password = reader.GetString()!;

                    ((Pop3ConfigurationEntity)ctx.Entity).Password = Pop3ConfigurationLogic.EncryptPassword(password);
                }
            });
        }
    }
}
