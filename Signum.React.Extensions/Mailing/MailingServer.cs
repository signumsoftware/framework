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
using Signum.Engine.Authorization;
using Signum.Entities.Word;
using Signum.React.Extensions.Templating;
using Microsoft.Exchange.WebServices.Data;
using Signum.Entities.Authorization;
using System.Net.Mail;

namespace Signum.React.Mailing
{
    public static class MailingServer
    {
        public static void Start(IApplicationBuilder app)
        {
            TypeHelpServer.Start(app);
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
            ReflectionServer.OverrideIsNamespaceAllowed.Add(typeof(ExchangeVersion).Namespace!, () => TypeAuthLogic.GetAllowed(typeof(EmailSenderConfigurationEntity)).MaxUI() > TypeAllowedBasic.None);
            ReflectionServer.OverrideIsNamespaceAllowed.Add(typeof(SmtpDeliveryMethod).Namespace!, () => TypeAuthLogic.GetAllowed(typeof(EmailSenderConfigurationEntity)).MaxUI() > TypeAllowedBasic.None);


            TemplatingServer.Start(app);

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
        }
    }
}
