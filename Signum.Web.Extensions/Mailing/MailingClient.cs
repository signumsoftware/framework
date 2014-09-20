#region usings
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
#endregion

namespace Signum.Web.Mailing
{
    public static class MailingClient
    {
        public static string ViewPrefix = "~/Mailing/Views/{0}.cshtml";
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/Mailing/Scripts/Mailing");

        private static QueryTokenDN ParseQueryToken(string tokenString, string queryRuntimeInfoInput)
        {
            if (tokenString.IsNullOrEmpty())
                return null;

            var queryRuntimeInfo = RuntimeInfo.FromFormValue(queryRuntimeInfoInput);
            var queryName = QueryLogic.ToQueryName(((Lite<QueryDN>)queryRuntimeInfo.ToLite()).InDB(q => q.Key));
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

            return new QueryTokenDN(QueryUtils.Parse(tokenString, qd, SubTokensOptions.CanElement));
        }

        public static void Start(bool newsletter, bool pop3Config)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                CultureInfoClient.Start();

                Navigator.RegisterArea(typeof(MailingClient));
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EmbeddedEntitySettings<EmailAttachmentDN>{ PartialViewName = e => ViewPrefix.Formato("EmailAttachment")},
                    new EntitySettings<EmailPackageDN>{ PartialViewName = e => ViewPrefix.Formato("EmailPackage")},
                    
                    new EntitySettings<EmailMessageDN>{ PartialViewName = e => ViewPrefix.Formato("EmailMessage"), AvoidValidateRequest = true },
                    
                    new EmbeddedEntitySettings<EmailAddressDN>{ PartialViewName = e => ViewPrefix.Formato("EmailAddress")},
                    new EmbeddedEntitySettings<EmailRecipientDN>{ PartialViewName = e => ViewPrefix.Formato("EmailRecipient")},
                    
                    new EmbeddedEntitySettings<EmailConfigurationDN> { PartialViewName = e => ViewPrefix.Formato("EmailConfiguration")},
                    new EntitySettings<SystemEmailDN>{ },
                    
                    new EntitySettings<EmailMasterTemplateDN> { PartialViewName = e => ViewPrefix.Formato("EmailMasterTemplate"), AvoidValidateRequest = true  },
                    new EmbeddedEntitySettings<EmailMasterTemplateMessageDN>
                    {
                        PartialViewName = e => ViewPrefix.Formato("EmailMasterTemplateMessage"),
                        MappingDefault = new EntityMapping<EmailMasterTemplateMessageDN>(true)
                            .SetProperty(emtm => emtm.MasterTemplate, ctx => 
                            {
                                return (EmailMasterTemplateDN)ctx.Parent.Parent.Parent.Parent.UntypedValue;
                            })
                    },
                    
                    new EntitySettings<EmailTemplateDN> { PartialViewName = e => ViewPrefix.Formato("EmailTemplate"), AvoidValidateRequest = true },
                    new EmbeddedEntitySettings<EmailTemplateMessageDN>() 
                    { 
                        PartialViewName = e => ViewPrefix.Formato("EmailTemplateMessage"),
                        MappingDefault = new EntityMapping<EmailTemplateMessageDN>(true)
                            .SetProperty(etm => etm.Template, ctx =>
                            {
                                return (EmailTemplateDN)ctx.Parent.Parent.Parent.Parent.UntypedValue;
                            })
                    },

                    new EmbeddedEntitySettings<EmailTemplateContactDN>() 
                    { 
                        PartialViewName = e => ViewPrefix.Formato("EmailTemplateContact"),
                        MappingDefault = new EntityMapping<EmailTemplateContactDN>(true)
                            .SetProperty(ec => ec.Token, ctx =>
                            {
                                string tokenStr = UserAssetsHelper.GetTokenString(ctx);
                                return ParseQueryToken(tokenStr, ctx.Parent.Parent.Parent.Inputs[TypeContextUtilities.Compose("Query", EntityBaseKeys.RuntimeInfo)]);
                            }),
                    },

                    new EmbeddedEntitySettings<EmailTemplateRecipientDN>() 
                    { 
                        PartialViewName = e => ViewPrefix.Formato("EmailTemplateRecipient"),
                        MappingDefault = new EntityMapping<EmailTemplateRecipientDN>(true)
                            .SetProperty(ec => ec.Token, ctx =>
                            {
                                string tokenStr = UserAssetsHelper.GetTokenString(ctx);

                                return ParseQueryToken(tokenStr, ctx.Parent.Parent.Parent.Parent.Inputs[TypeContextUtilities.Compose("Query", EntityBaseKeys.RuntimeInfo)]);
                            })
                    },

                    new EntitySettings<SmtpConfigurationDN> { PartialViewName = e => ViewPrefix.Formato("SmtpConfiguration") },
                    new EmbeddedEntitySettings<ClientCertificationFileDN> { PartialViewName = e => ViewPrefix.Formato("ClientCertificationFile")},
                });

                OperationClient.AddSettings(new List<OperationSettings>
                {
                    new EntityOperationSettings<EmailTemplateDN>(EmailMessageOperation.CreateMailFromTemplate)
                    {
                        Group = EntityOperationGroup.None,
                        Click = ctx => Module["createMailFromTemplate"](ctx.Options(), JsFunction.Event, 
                            new FindOptions(((EmailTemplateDN)ctx.Entity).Query.ToQueryName()).ToJS(ctx.Prefix, "New"), 
                            ctx.Url.Action((MailingController mc)=>mc.CreateMailFromTemplateAndEntity()))
                    }
                });

                if (newsletter)
                {
                    Navigator.AddSettings(new List<EntitySettings>
                    {
                        new EntitySettings<NewsletterDN> { PartialViewName = e => ViewPrefix.Formato("Newsletter"), AvoidValidateRequest = true},
                        new EntitySettings<NewsletterDeliveryDN> { PartialViewName = e => ViewPrefix.Formato("NewsletterDelivery") },
                    });

                    OperationClient.AddSettings(new List<OperationSettings>
                    {
                        new EntityOperationSettings<NewsletterDN>(NewsletterOperation.RemoveRecipients)
                        {
                            Click = ctx => Module["removeRecipients"](ctx.Options(),
                                new FindOptions(typeof(NewsletterDeliveryDN), "Newsletter", ctx.Entity).ToJS(ctx.Prefix, "New"),
                                ctx.Url.Action((MailingController mc)=>mc.RemoveRecipientsExecute()))
                        },

                        new EntityOperationSettings<NewsletterDN>(NewsletterOperation.Send)
                        {
                            Group = EntityOperationGroup.None,
                        }
                    });
                }

                if (pop3Config)
                    Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<Pop3ConfigurationDN> { PartialViewName = e => ViewPrefix.Formato("Pop3Configuration") },
                    new EntitySettings<Pop3ReceptionDN> { PartialViewName = e => ViewPrefix.Formato("Pop3Reception") },
                });


                TasksGetWebMailBody += WebMailProcessor.ReplaceUntrusted;
                TasksGetWebMailBody += WebMailProcessor.CidToFilePath;

                TasksSetWebMailBody += WebMailProcessor.AssertNoUntrusted;
                TasksSetWebMailBody += WebMailProcessor.FilePathToCid;

                Navigator.EntitySettings<EmailMessageDN>().MappingMain.AsEntityMapping()
                    .RemoveProperty(a => a.Body)
                    .SetProperty(a => a.Body, ctx =>
                    {
                        var email = ((EmailMessageDN)ctx.Parent.UntypedValue);

                        return SetWebMailBody(ctx.Value, new WebMailOptions
                        {
                             Attachments = email.Attachments,
                             UntrustedImage = null,
                             Url = RouteHelper.New(),
                        });
                    }); 
            }
        }

        public static QueryTokenBuilderSettings GetQueryTokenBuilderSettings(QueryDescription qd, SubTokensOptions options)
        {
            return new QueryTokenBuilderSettings(qd, options)
            {
                ControllerUrl = RouteHelper.New().Action("NewSubTokensCombo", "Mailing"),
                Decorators = MailingDecorators,
                RequestExtraJSonData = null,
            };
        }

        static void MailingDecorators(QueryToken qt, HtmlTag option)
        {
            string canIf = CanIf(qt);
            if (canIf.HasText())
                option.Attr("data-if", canIf);

            string canForeach = CanForeach(qt);
            if (canForeach.HasText())
                option.Attr("data-foreach", canForeach);

            string canAny = CanAny(qt);
            if (canAny.HasText())
                option.Attr("data-any", canAny);
        }

        static string CanIf(QueryToken token)
        {
            if (token == null)
                return EmailTemplateCanAddTokenMessage.NoColumnSelected.NiceToString();

            if (token.Type != typeof(string) && token.Type != typeof(byte[]) && token.Type.ElementType() != null)
                return EmailTemplateCanAddTokenMessage.YouCannotAddIfBlocksOnCollectionFields.NiceToString();

            if (token.HasAllOrAny())
                return EmailTemplateCanAddTokenMessage.YouCannotAddBlocksWithAllOrAny.NiceToString();

            return null;
        }

        static string CanForeach(QueryToken token)
        {
            if (token == null)
                return EmailTemplateCanAddTokenMessage.NoColumnSelected.NiceToString();

            if (token.Type != typeof(string) && token.Type != typeof(byte[]) && token.Type.ElementType() != null)
                return EmailTemplateCanAddTokenMessage.YouHaveToAddTheElementTokenToUseForeachOnCollectionFields.NiceToString();

            if (token.Key != "Element" || token.Parent == null || token.Parent.Type.ElementType() == null)
                return EmailTemplateCanAddTokenMessage.YouCanOnlyAddForeachBlocksWithCollectionFields.NiceToString();

            if (token.HasAllOrAny())
                return EmailTemplateCanAddTokenMessage.YouCannotAddBlocksWithAllOrAny.NiceToString();

            return null; 
        }

        static string CanAny(QueryToken token)
        {
            if (token == null)
                return EmailTemplateCanAddTokenMessage.NoColumnSelected.NiceToString();

            if (token.HasAllOrAny())
                return EmailTemplateCanAddTokenMessage.YouCannotAddBlocksWithAllOrAny.NiceToString();

            return null;
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
