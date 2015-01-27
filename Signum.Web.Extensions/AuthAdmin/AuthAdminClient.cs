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
using Signum.Engine.Basics;
using Signum.Web.Basic;
using Signum.Web.Omnibox;

namespace Signum.Web.AuthAdmin
{
    public static class AuthAdminClient
    {
        public static string ViewPrefix = "~/authAdmin/Views/{0}.cshtml";
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/AuthAdmin/Scripts/AuthAdmin");

        public static void Start(bool types, bool properties, bool queries, bool operations, bool permissions)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(AuthAdminClient));
                if (Navigator.Manager.EntitySettings.ContainsKey(typeof(UserEntity)))
                    Navigator.EntitySettings<UserEntity>().PartialViewName = _ => ViewPrefix.FormatWith("User");
                else
                    Navigator.AddSetting(new EntitySettings<UserEntity> { PartialViewName = _ => ViewPrefix.FormatWith("User") });

                Navigator.EntitySettings<UserEntity>().MappingMain.AsEntityMapping().RemoveProperty(a => a.PasswordHash);

                if (Navigator.Manager.EntitySettings.ContainsKey(typeof(RoleEntity)))
                    Navigator.EntitySettings<RoleEntity>().PartialViewName = _ => ViewPrefix.FormatWith("Role");
                else
                    Navigator.AddSetting(new EntitySettings<RoleEntity> { PartialViewName = _ => ViewPrefix.FormatWith("Role") });

                if (types)
                {
                    RegisterTypes();
                }

                if (properties)
                    Register<PropertyRulePack, PropertyAllowedRule, PropertyRouteEntity, PropertyAllowed, string>("properties", a => a.Resource.Path,
                        Mapping.New<PropertyAllowed>(), true);

                if (queries)
                {
                    QueryClient.Start();

                    Register<QueryRulePack, QueryAllowedRule, QueryEntity, bool, string>("queries", a => a.Resource.Key,
                        Mapping.New<bool>(), true);
                }

                if (operations)
                    Register<OperationRulePack, OperationAllowedRule, OperationSymbol, OperationAllowed, OperationSymbol>("operations", a => a.Resource,
                        Mapping.New<OperationAllowed>(), true);

                if (permissions)
                    Register<PermissionRulePack, PermissionAllowedRule, PermissionSymbol, bool, PermissionSymbol>("permissions", a => a.Resource,
                        Mapping.New<bool>(), false);

                LinksClient.RegisterEntityLinks<RoleEntity>((role, ctx) =>
                     !BasicPermission.AdminRules.IsAuthorized() ? null :
                     new[]
                     {
                         types ? new QuickLinkAction(AuthAdminMessage.TypeRules, RouteHelper.New().Action((AuthAdminController c)=>c.Types(role))): null,
                         permissions ? new QuickLinkAction(AuthAdminMessage.PermissionRules, RouteHelper.New().Action((AuthAdminController c)=>c.Permissions(role))): null,
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

        static void Register<T, AR, R, A, K>(string partialViewName, Expression<Func<AR, K>> getKey, Mapping<A> allowedMapping, bool embedded)
            where T : BaseRulePack<AR>
            where AR : AllowedRule<R, A>, new()
            where R : Entity
        {
            if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(R)))
                Navigator.AddSetting(new EntitySettings<R>());

            string viewPrefix = "~/authAdmin/Views/{0}.cshtml";
            Navigator.AddSetting(new EmbeddedEntitySettings<T>
            {
                PartialViewName = e => viewPrefix.FormatWith(partialViewName),
                MappingDefault = new EntityMapping<T>(false)
                    .SetProperty(m => m.Rules,
                        new MListDictionaryMapping<AR, K>(getKey,
                            new EntityMapping<AR>(false).SetProperty(p => p.Allowed, allowedMapping)))
            });

            RegisterSaveButton<T>(partialViewName, embedded);
        }

        static void RegisterTypes()
        {
            Navigator.AddSetting(new EmbeddedEntitySettings<TypeConditionRule>());

            string viewPrefix = "~/authAdmin/Views/{0}.cshtml";
            Navigator.AddSetting(new EmbeddedEntitySettings<TypeRulePack>
            {
                PartialViewName = e => viewPrefix.FormatWith("types"),
                MappingDefault = new EntityMapping<TypeRulePack>(false)
                    .SetProperty(m => m.Rules,
                        new MListDictionaryMapping<TypeAllowedRule, TypeEntity>(a => a.Resource,
                            new EntityMapping<TypeAllowedRule>(false)
                            .SetProperty(p => p.Allowed, ctx => new TypeAllowedAndConditions(
                                ParseTypeAllowed(ctx.Inputs.SubDictionary("Fallback")),
                                ctx.Inputs.SubDictionary("Conditions").IndexSubDictionaries().Select(d =>
                                    new TypeConditionRule(
                                        SymbolLogic<TypeConditionSymbol>.ToSymbol(d["ConditionName"]),
                                        ParseTypeAllowed(d.SubDictionary("Allowed")))
                                   ).ToReadOnly()))
                        ))
            });

            RegisterSaveButton<TypeRulePack>("types", false);
        }

        private static void RegisterSaveButton<T>(string partialViewName, bool embedded)
            where T : ModifiableEntity
        {
            ButtonBarEntityHelper.RegisterEntityButtons<T>((ctx, entity) =>
            {
                if (TypeAuthLogic.GetAllowed(PackToRule.GetOrThrow(typeof(T))).MaxUI() >= TypeAllowedBasic.Modify)
                    return new[] 
                    { 
                        new ToolBarButton (ctx.Prefix,partialViewName)
                        { 
                            Text = AuthMessage.Save.NiceToString(),
                            Style = BootstrapStyle.Primary,
                            OnClick =  embedded?
                                Module["postDialog"](ctx.Url.Action( "save" +  partialViewName, "AuthAdmin"), ctx.Prefix):
                                Module["submitPage"](ctx.Url.Action( partialViewName, "AuthAdmin"), ctx.Prefix),
                        }
                    };

                return new ToolBarButton[] { };

            });
        }

        static Dictionary<Type, Type> PackToRule = new Dictionary<Type, Type> 
        {
            {typeof(TypeRulePack),typeof(RuleTypeEntity)},
            {typeof(PropertyRulePack),typeof(RulePropertyEntity)},
            {typeof(QueryRulePack),typeof(RuleQueryEntity)},
            {typeof(OperationRulePack),typeof(RuleOperationEntity)},
            {typeof(PermissionRulePack),typeof(RulePermissionEntity)},        
        };
    }
}
