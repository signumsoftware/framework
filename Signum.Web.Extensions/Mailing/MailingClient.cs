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
using Signum.Entities.Files;
using Signum.Engine.Mailing;
using System.Web.UI;
using System.IO;
using Signum.Entities.Mailing;
using System.Web.Routing;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserQueries;
using Signum.Web.Operations;
using Signum.Web.UserQueries;
using System.Text.RegularExpressions;
using Signum.Entities.UserAssets;
using Signum.Web.UserAssets;
using Signum.Web.Basic;
using Signum.Entities.Processes;
using Signum.Web.Cultures;
using Signum.Web.Templating;
using Signum.Web.Omnibox;
using Signum.Engine.Authorization;

namespace Signum.Web.Mailing
{
    public static class MailingClient
    {
        public static string ViewPrefix = "~/Mailing/Views/{0}.cshtml";
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/Mailing/Scripts/Mailing");
        public static JsModule AsyncEmailSenderModule = new JsModule("Extensions/Signum.Web.Extensions/Mailing/Scripts/AsyncEmailSender");

        private static QueryTokenEntity ParseQueryToken(string tokenString, string queryRuntimeInfoInput)
        {
            if (tokenString.IsNullOrEmpty())
                return null;

            var queryRuntimeInfo = RuntimeInfo.FromFormValue(queryRuntimeInfoInput);
            var queryName = QueryLogic.ToQueryName(((Lite<QueryEntity>)queryRuntimeInfo.ToLite()).InDB(q => q.Key));
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

            return new QueryTokenEntity(QueryUtils.Parse(tokenString, qd, SubTokensOptions.CanElement));
        }

        public static void Start(bool smtpConfig, bool newsletter, bool pop3Config)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                CultureInfoClient.Start();

                Navigator.RegisterArea(typeof(MailingClient));
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EmbeddedEntitySettings<EmailAttachmentEntity>{ PartialViewName = e => ViewPrefix.FormatWith("EmailAttachment")},
                    new EntitySettings<EmailPackageEntity>{ PartialViewName = e => ViewPrefix.FormatWith("EmailPackage")},
                    
                    new EntitySettings<EmailMessageEntity>{ PartialViewName = e => ViewPrefix.FormatWith("EmailMessage"), AvoidValidateRequest = true },
                    
                    new EmbeddedEntitySettings<EmailAddressEntity>{ PartialViewName = e => ViewPrefix.FormatWith("EmailAddress")},
                    new EmbeddedEntitySettings<EmailRecipientEntity>{ PartialViewName = e => ViewPrefix.FormatWith("EmailRecipient")},
                    
                    new EmbeddedEntitySettings<EmailConfigurationEntity> { PartialViewName = e => ViewPrefix.FormatWith("EmailConfiguration")},
                    new EntitySettings<SystemEmailEntity>{ },
                    
                    new EntitySettings<EmailMasterTemplateEntity> { PartialViewName = e => ViewPrefix.FormatWith("EmailMasterTemplate"), AvoidValidateRequest = true  },
                    new EmbeddedEntitySettings<EmailMasterTemplateMessageEntity>
                    {
                        PartialViewName = e => ViewPrefix.FormatWith("EmailMasterTemplateMessage"),
                        MappingDefault = new EntityMapping<EmailMasterTemplateMessageEntity>(true)
                            .SetProperty(emtm => emtm.MasterTemplate, ctx => 
                            {
                                return (EmailMasterTemplateEntity)ctx.Parent.Parent.Parent.Parent.UntypedValue;
                            })
                    },
                    
                    new EntitySettings<EmailTemplateEntity> { PartialViewName = e => ViewPrefix.FormatWith("EmailTemplate"), AvoidValidateRequest = true },
                    new EmbeddedEntitySettings<EmailTemplateMessageEntity>() 
                    { 
                        PartialViewName = e => ViewPrefix.FormatWith("EmailTemplateMessage"),
                        MappingDefault = new EntityMapping<EmailTemplateMessageEntity>(true)
                            .SetProperty(etm => etm.Template, ctx =>
                            {
                                return (EmailTemplateEntity)ctx.Parent.Parent.Parent.Parent.UntypedValue;
                            })
                    },

                    new EmbeddedEntitySettings<EmailTemplateContactEntity>() 
                    { 
                        PartialViewName = e => ViewPrefix.FormatWith("EmailTemplateContact"),
                        MappingDefault = new EntityMapping<EmailTemplateContactEntity>(true)
                            .SetProperty(ec => ec.Token, ctx =>
                            {
                                string tokenStr = UserAssetsHelper.GetTokenString(ctx);
                                return ParseQueryToken(tokenStr, ctx.Parent.Parent.Parent.Inputs[TypeContextUtilities.Compose("Query", EntityBaseKeys.RuntimeInfo)]);
                            }),
                    },

                    new EmbeddedEntitySettings<EmailTemplateRecipientEntity>() 
                    { 
                        PartialViewName = e => ViewPrefix.FormatWith("EmailTemplateRecipient"),
                        MappingDefault = new EntityMapping<EmailTemplateRecipientEntity>(true)
                            .SetProperty(ec => ec.Token, ctx =>
                            {
                                string tokenStr = UserAssetsHelper.GetTokenString(ctx);

                                return ParseQueryToken(tokenStr, ctx.Parent.Parent.Parent.Parent.Inputs[TypeContextUtilities.Compose("Query", EntityBaseKeys.RuntimeInfo)]);
                            })
                    },
                });

                OperationClient.AddSettings(new List<OperationSettings>
                {
                    new EntityOperationSettings<EmailTemplateEntity>(EmailMessageOperation.CreateMailFromTemplate)
                    {
                        Group = EntityOperationGroup.None,
                        Click = ctx => Module["createMailFromTemplate"](ctx.Options(), JsFunction.Event, 
                            new FindOptions(((EmailTemplateEntity)ctx.Entity).Query.ToQueryName()).ToJS(ctx.Prefix, "New"), 
                            ctx.Url.Action((MailingController mc)=>mc.CreateMailFromTemplateAndEntity()))
                    }
                });

                if (smtpConfig)
                {
                    Navigator.AddSettings(new List<EntitySettings>
                    {
                        new EntitySettings<SmtpConfigurationEntity> { PartialViewName = e => ViewPrefix.FormatWith("SmtpConfiguration") },
                        new EmbeddedEntitySettings<SmtpNetworkDeliveryEntity> { PartialViewName = e => ViewPrefix.FormatWith("SmtpNetworkDelivery") },
                        new EmbeddedEntitySettings<ClientCertificationFileEntity> { PartialViewName = e => ViewPrefix.FormatWith("ClientCertificationFile")},
                    });
                }

                if (newsletter)
                {
                    Navigator.AddSettings(new List<EntitySettings>
                    {
                        new EntitySettings<NewsletterEntity> { PartialViewName = e => ViewPrefix.FormatWith("Newsletter"), AvoidValidateRequest = true},
                        new EntitySettings<NewsletterDeliveryEntity> { PartialViewName = e => ViewPrefix.FormatWith("NewsletterDelivery") },
                    });

                    OperationClient.AddSettings(new List<OperationSettings>
                    {
                        new EntityOperationSettings<NewsletterEntity>(NewsletterOperation.RemoveRecipients)
                        {
                            Click = ctx => Module["removeRecipients"](ctx.Options(),
                                new FindOptions(typeof(NewsletterDeliveryEntity), "Newsletter", ctx.Entity).ToJS(ctx.Prefix, "New"),
                                ctx.Url.Action((MailingController mc)=>mc.RemoveRecipientsExecute()))
                        },

                        new EntityOperationSettings<NewsletterEntity>(NewsletterOperation.Send)
                        {
                            Group = EntityOperationGroup.None,
                        }
                    });
                }

                if (pop3Config)
                    Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<Pop3ConfigurationEntity> { PartialViewName = e => ViewPrefix.FormatWith("Pop3Configuration") },
                    new EntitySettings<Pop3ReceptionEntity> { PartialViewName = e => ViewPrefix.FormatWith("Pop3Reception") },
                });


                TasksGetWebMailBody += WebMailProcessor.ReplaceUntrusted;
                TasksGetWebMailBody += WebMailProcessor.CidToFilePath;

                TasksSetWebMailBody += WebMailProcessor.AssertNoUntrusted;
                TasksSetWebMailBody += WebMailProcessor.FilePathToCid;

                Navigator.EntitySettings<EmailMessageEntity>().MappingMain.AsEntityMapping()
                    .RemoveProperty(a => a.Body)
                    .SetProperty(a => a.Body, ctx =>
                    {
                        if (!ctx.HasInput)
                            return ctx.None();

                        var email = ((EmailMessageEntity)ctx.Parent.UntypedValue);

                        return SetWebMailBody(ctx.Input, new WebMailOptions
                        {
                             Attachments = email.Attachments,
                             UntrustedImage = null,
                             Url = RouteHelper.New(),
                        });
                    });

                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("AsyncEmailPanel",
                    () => AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel.IsAuthorized(),
                    uh => uh.Action((AsyncEmailSenderController pc) => pc.View())));
            }
        }

        public static QueryTokenBuilderSettings GetQueryTokenBuilderSettings(QueryDescription qd, SubTokensOptions options)
        {
            return new QueryTokenBuilderSettings(qd, options)
            {
                ControllerUrl = RouteHelper.New().Action("NewSubTokensCombo", "Mailing"),
                Decorators = TemplatingClient.TemplatingDecorators,
                RequestExtraJSonData = null,
            };
        }

        public static Func<string, WebMailOptions, string> TasksSetWebMailBody; 
        public static string SetWebMailBody(string body, WebMailOptions options)
        {
            if (body == null)
                return null;

            foreach (var f in TasksSetWebMailBody.GetInvocationListTyped())
                body = f(body, options); 

            return body;
        }

        public static Func<string, WebMailOptions, string> TasksGetWebMailBody;
        public static string GetWebMailBody(string body, WebMailOptions options)
        {
            if (body == null)
                return null;

            foreach (var f in TasksGetWebMailBody.GetInvocationListTyped())
                body = f(body, options);

            return body;
        }
    }
}
