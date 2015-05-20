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
using Signum.Web.Maps;

namespace Signum.Web.AuthAdmin
{
    public static class AuthAdminClient
    {
        public static string ViewPrefix = "~/authAdmin/Views/{0}.cshtml";
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/AuthAdmin/Scripts/AuthAdmin");
        public static JsModule ColorModule = new JsModule("Extensions/Signum.Web.Extensions/AuthAdmin/Scripts/AuthAdminColors");

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

                MapClient.GetColorProviders += GetMapColors;
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



        static MapColorProvider[] GetMapColors()
        {
            if(!BasicPermission.AdminRules.IsAuthorized())
                return new MapColorProvider[0];

            var roleRules = AuthLogic.RolesInOrder().ToDictionary(r=>r, r=>TypeAuthLogic.GetTypeRules(r).Rules.ToDictionary(a=>Navigator.ResolveWebTypeName(a.Resource.ToType()), a=>a.Allowed));

            return roleRules.Keys.Select((r, i) => new MapColorProvider
            {
                Name = "role-" + r.Key(),
                NiceName = "Role - " + r.ToString(),
                GetJsProvider = ColorModule["authAdminColor"](MapClient.NodesConstant, "role-" + r.Key()),
                AddExtra = t =>
                {
                    TypeAllowedAndConditions tac = roleRules[r].TryGetC(t.webTypeName);

                    if (tac == null)
                        return;

                    t.extra["role-" + r.Key() + "-ui"] = GetName(ToStringList(tac, userInterface: true));
                    t.extra["role-" + r.Key() + "-db"] = GetName(ToStringList(tac, userInterface: false));
                    t.extra["role-" + r.Key() + "-tooltip"] = ToString(tac.Fallback) + "\n" + (tac.Conditions.IsNullOrEmpty() ? null :
                        tac.Conditions.ToString(a => a.TypeCondition.NiceToString() + ": " + ToString(a.Allowed), "\n") + "\n");
                },
                Defs = i == 0 ? GetAllGradients(roleRules) : null,
                Order = 10,
            }).ToArray();
        }

        private static string ToString(TypeAllowed? typeAllowed)
        {
            if (typeAllowed == null)
                return "MERGE ERROR!";

            if (typeAllowed.Value.GetDB() == typeAllowed.Value.GetUI())
                return typeAllowed.Value.GetDB().NiceToString();

            return "DB {0} / UI {1}".FormatWith(typeAllowed.Value.GetDB().NiceToString(), typeAllowed.Value.GetUI().NiceToString()); 
        }

        static string GetName(List<TypeAllowedBasic?> list)
        {
            return "auth-" + list.ToString(a => a == null ? "Error" : a.ToString(), "-");
        }

        static MvcHtmlString GetAllGradients(Dictionary<Lite<RoleEntity>, Dictionary<string, TypeAllowedAndConditions>> roleRules)
        {
            var distinct = roleRules.Values.SelectMany(a => a.Values).SelectMany(tac => new[]{
                ToStringList(tac, userInterface: true),
                ToStringList(tac, userInterface: false),
            }).Distinct(a => a.ToString("-"));

            return new HtmlStringBuilder(distinct.Select(list => GradientDef(list))).ToHtml();
        }

        private static List<TypeAllowedBasic?> ToStringList(TypeAllowedAndConditions tac, bool userInterface)
        {
            List<TypeAllowedBasic?> result = new List<TypeAllowedBasic?>();
            result.Add(tac.Fallback == null ? (TypeAllowedBasic?)null : tac.Fallback.Value.Get(userInterface));

            foreach (var c in tac.Conditions)
                result.Add(c.Allowed.Get(userInterface));

            return result;
        }

        static MvcHtmlString GradientDef(List<TypeAllowedBasic?> list)
        {
            return MvcHtmlString.Create(@"
<linearGradient id=""" + GetName(list) + @""" x1=""0%"" y1=""0%"" x2=""100%"" y2=""0%"">" +
list.Select((l, i) =>
@"<stop offset=""" + (100 * i / list.Count) + @"%"" style=""stop-color:" + Color(l) + @""" />" +
@"<stop offset=""" + ((100 * (i + 1) / list.Count) - 1) + @"%"" style=""stop-color:" + Color(l) + @""" />"
).ToString("\r\n") +
"</linearGradient>");
        }

        private static string Color(TypeAllowedBasic? typeAllowedBasic)
        {
            switch (typeAllowedBasic)
            {
                case null: return "black";
                case TypeAllowedBasic.Create: return "#0066FF";
                case TypeAllowedBasic.Modify: return "green";
                case TypeAllowedBasic.Read: return "gold";
                case TypeAllowedBasic.None: return "red";
                default: throw new InvalidOperationException();
            }
        }
    }
}
