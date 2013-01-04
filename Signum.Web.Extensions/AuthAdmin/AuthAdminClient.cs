using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using System.Reflection;
using Signum.Web.Operations;
using Signum.Entities;
using System.Web.Mvc;
using System.Diagnostics;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using Signum.Engine.Maps;
using System.Web.Routing;
using Signum.Web.Extensions.Properties;
using Signum.Engine.Basics;
using Signum.Web.Basic;
using Signum.Web.Omnibox;

namespace Signum.Web.AuthAdmin
{
    public static class AuthAdminClient
    {
        public static string ViewPrefix = "~/authAdmin/Views/{0}.cshtml";

        public static void Start(bool types, bool properties, bool queries, bool operations, bool permissions, bool facadeMethods)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(AuthAdminClient));
                if (Navigator.Manager.EntitySettings.ContainsKey(typeof(UserDN)))
                    Navigator.EntitySettings<UserDN>().PartialViewName = _ => ViewPrefix.Formato("User");
                else
                    Navigator.AddSetting(new EntitySettings<UserDN> { PartialViewName = _ => ViewPrefix.Formato("User") });

                if (Navigator.Manager.EntitySettings.ContainsKey(typeof(RoleDN)))
                    Navigator.EntitySettings<RoleDN>().PartialViewName = _ => ViewPrefix.Formato("Role");
                else
                    Navigator.AddSetting(new EntitySettings<RoleDN> { PartialViewName = _ => ViewPrefix.Formato("Role") });

                if (types)
                {
                    RegisterTypes();
                }

                if (properties)
                    Register<PropertyRulePack, PropertyAllowedRule, PropertyDN, PropertyAllowed, string>("properties", a => a.Resource.Path,
                        Mapping.New<PropertyAllowed>(), "Resource_Path", true);

                if (queries)
                {
                    QueryClient.Start();

                    Register<QueryRulePack, QueryAllowedRule, QueryDN, bool, string>("queries", a => a.Resource.Key,
                        Mapping.New<bool>(), "Resource_Key", true);
                }

                if (operations)
                    Register<OperationRulePack, OperationAllowedRule, OperationDN, OperationAllowed, OperationDN>("operations", a => a.Resource,
                        Mapping.New<OperationAllowed>(), "Resource", true);

                if (permissions)
                    Register<PermissionRulePack, PermissionAllowedRule, PermissionDN, bool, PermissionDN>("permissions", a => a.Resource,
                        Mapping.New<bool>(), "Resource", false);


                if (facadeMethods)
                    Register<FacadeMethodRulePack, FacadeMethodAllowedRule, FacadeMethodDN, bool, string>("facadeMethods", a => a.Resource.ToString(),
                        Mapping.New<bool>(), "Resource_Key", false);

                QuickLinkWidgetHelper.RegisterEntityLinks<RoleDN>((RoleDN entity, string partialViewName, string prefix) =>
                     entity.IsNew || !BasicPermission.AdminRules.IsAuthorized() ? null :
                     new[]
                     {
                         types ? new QuickLinkAction(Resources._0Rules.Formato(typeof(TypeDN).NiceName()), RouteHelper.New().Action((AuthAdminController c)=>c.Types(entity.ToLite()))): null,
                         permissions ? new QuickLinkAction(Resources._0Rules.Formato(typeof(PermissionDN).NiceName()), RouteHelper.New().Action((AuthAdminController c)=>c.Permissions(entity.ToLite()))): null,
                         facadeMethods ? new QuickLinkAction(Resources._0Rules.Formato(typeof(FacadeMethodDN).NiceName()), RouteHelper.New().Action((AuthAdminController c)=>c.FacadeMethods(entity.ToLite()))): null
                     });

                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("DownloadAuthRules",
                    () => BasicPermission.AdminRules.IsAuthorized(),
                    uh => uh.Action((AuthAdminController aac) => aac.Export())));
            }
        }

        static TypeAllowed ParseTypeAllowed(IDictionary<string, string> dic)
        {
            return TypeAllowedExtensions.Create(
                Mapping.ParseHtmlBool(dic["Create"]),
                Mapping.ParseHtmlBool(dic["Modify"]),
                Mapping.ParseHtmlBool(dic["Read"]),
                Mapping.ParseHtmlBool(dic["None"]));
        }

        static void Register<T, AR, R, A, K>(string partialViewName, Func<AR, K> getKey, Mapping<A> allowedMapping, string getKeyRoute, bool embedded)
            where T : BaseRulePack<AR>
            where AR : AllowedRule<R, A>, new()
            where R : IdentifiableEntity
        {
            if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(R)))
                Navigator.AddSetting(new EntitySettings<R>());

            string viewPrefix = "~/authAdmin/Views/{0}.cshtml";
            Navigator.AddSetting(new EmbeddedEntitySettings<T>
            {
                PartialViewName = e => viewPrefix.Formato(partialViewName),
                MappingDefault = new EntityMapping<T>(false)
                    .CreateProperty(m => m.DefaultRule)
                    .SetProperty(m => m.Rules,
                        new MListDictionaryMapping<AR, K>(getKey, getKeyRoute)
                        {
                            ElementMapping = new EntityMapping<AR>(false).SetProperty(p => p.Allowed, allowedMapping)
                        })
            });

            RegisterSaveButton<T>(partialViewName, embedded);
        }

        static void RegisterTypes()
        {
            Navigator.AddSetting(new EmbeddedEntitySettings<TypeConditionRule>());

            string viewPrefix = "~/authAdmin/Views/{0}.cshtml";
            Navigator.AddSetting(new EmbeddedEntitySettings<TypeRulePack>
            {
                PartialViewName = e => viewPrefix.Formato("types"),
                MappingDefault = new EntityMapping<TypeRulePack>(false)
                    .CreateProperty(m => m.DefaultRule)
                    .SetProperty(m => m.Rules,
                        new MListDictionaryMapping<TypeAllowedRule, TypeDN>(a => a.Resource, "Resource")
                        {
                            ElementMapping = new EntityMapping<TypeAllowedRule>(false)
                            .SetProperty(p => p.Allowed, ctx => new TypeAllowedAndConditions(
                                ParseTypeAllowed(ctx.Inputs.SubDictionary("Fallback")),
                                ctx.Inputs.SubDictionary("Conditions").IndexSubDictionaries().Select(d =>
                                    new TypeConditionRule(
                                        MultiEnumLogic<TypeConditionNameDN>.ToEnum(d["ConditionName"]),
                                        ParseTypeAllowed(d.SubDictionary("Allowed")))
                                   ).ToReadOnly()))
                        })
            });

            RegisterSaveButton<TypeRulePack>("types", false);
        }

        private static void RegisterSaveButton<T>(string partialViewName, bool embedded)
            where T : ModifiableEntity
        {
            ButtonBarEntityHelper.RegisterEntityButtons<T>((ctx, entity) =>
                new[] { new ToolBarButton { 
                    OnClick = (embedded ? "SF.Auth.postDialog('{0}', '{1}')" :  "SF.submit('{0}', '{1}')").Formato(
                        new UrlHelper(ctx.ControllerContext.RequestContext).Action((embedded? "save" : "") +  partialViewName, "AuthAdmin"), ctx.Prefix), 
                    Text = Signum.Web.Properties.Resources.Save,
                    DivCssClass = ToolBarButton.DefaultEntityDivCssClass 
                } 
                });
        }

    }
}
