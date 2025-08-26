using Microsoft.AspNetCore.Builder;
using System.Net.Mail;
using Signum.Utilities.Reflection;
using System.Text.Json;
using Signum.Mailing.Templates;
using Signum.Authorization.Rules;
using Signum.Authorization;
using Signum.API;
using Signum.API.Json;
using Signum.Templating;
using Signum.API.Controllers;
using Signum.Eval.TypeHelp;

namespace Signum.Mailing;

public static class MailingServer
{
    public static void Start(WebServerBuilder wsb)
    {
        if (wsb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        TemplatingServer.TemplateTokenMessageAllowed += () => TypeAuthLogic.GetAllowed(typeof(EmailTemplateEntity)).MaxUI() > TypeAllowedBasic.None;
        ReflectionServer.OverrideIsNamespaceAllowed.Add(typeof(SmtpDeliveryMethod).Namespace!, () => TypeAuthLogic.GetAllowed(typeof(EmailSenderConfigurationEntity)).MaxUI() > TypeAllowedBasic.None);


        TemplatingServer.Start(wsb);

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
                try
                {
                    var templates = EmailTemplateLogic.GetApplicableEmailTemplates(type, null, EmailTemplateVisibleOn.Query);

                    if (templates.HasItems())
                        qd.Extension.Add("emailTemplates", templates);
                }
                catch (Exception e)
                {
                    e.LogException(); //An error could make the applicaiton unusable
                    qd.Extension.Add("emailTemplates", "error");
                }
            }
        };



        //if (Schema.Current.Tables.ContainsKey(typeof(SmtpEmailServiceEntity)))
        //{
        //    var piPassword = ReflectionTools.GetPropertyInfo((SmtpNetworkDeliveryEmbedded e) => e.Password);
        //    var pcs = SignumServer.WebEntityJsonConverterFactory.GetPropertyConverters(typeof(SmtpNetworkDeliveryEmbedded));
        //    pcs.GetOrThrow("password").CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) => { };
        //    pcs.Add("newPassword", new PropertyConverter
        //    {
        //        AvoidValidate = true,
        //        CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) => { },
        //        CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
        //        {
        //            ctx.Factory.AssertCanWrite(ctx.ParentPropertyRoute.Add(piPassword), ctx.Entity);

        //            var password = reader.GetString()!;

        //            ((SmtpNetworkDeliveryEmbedded)ctx.Entity).Password = EmailSenderConfigurationLogic.EncryptPassword(password);
        //        }
        //    });
        //}


    }
}
