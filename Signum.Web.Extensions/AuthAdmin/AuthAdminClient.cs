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
using Signum.Web.Properties;
using System.Diagnostics;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using Signum.Entities.Operations;
using System.Linq.Expressions;
using Signum.Engine.Maps;
using System.Web.Routing;

namespace Signum.Web.AuthAdmin
{
    public static class AuthAdminClient
    {
        public static string ViewPrefix = "~/authAdmin/Views/{0}.cshtml";

        public static void Start(bool types, bool properties, bool queries, bool operations, bool permissions, bool facadeMethods, bool entityGroups)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(AuthAdminClient));

                if (Navigator.Manager.EntitySettings.ContainsKey(typeof(UserDN)))
                    Navigator.EntitySettings<UserDN>().PartialViewName = _ => ViewPrefix.Formato("User");
                else
                    Navigator.AddSetting(new EntitySettings<UserDN>(EntityType.Default) { PartialViewName = _ => ViewPrefix.Formato("User") });

                if (Navigator.Manager.EntitySettings.ContainsKey(typeof(RoleDN)))
                    Navigator.EntitySettings<RoleDN>().PartialViewName = _ => ViewPrefix.Formato("Role"); 
                else
                    Navigator.AddSetting(new EntitySettings<RoleDN>(EntityType.Admin) { PartialViewName = _ => ViewPrefix.Formato("Role") });

                if (types)
                {
                    Register<TypeRulePack, TypeAllowedRule, TypeDN, TypeAllowed, TypeDN>("types", a => a.Resource, 
                        ctx=> ParseTypeAllowed(ctx.Inputs, null), "Resource", false);
                }

                if (properties)
                    Register<PropertyRulePack, PropertyAllowedRule, PropertyDN, PropertyAllowed, string>("properties", a => a.Resource.Path,
                        Mapping.New<PropertyAllowed>(), "Resource_Path", true);

                if (queries)
                {
                    Navigator.Manager.EntitySettings.Add(typeof(QueryDN), new EntitySettings<QueryDN>(EntityType.Default));
                    Register<QueryRulePack, QueryAllowedRule, QueryDN, bool, string>("queries", a => a.Resource.Key,
                        Mapping.New<bool>(), "Resource_Key", true);
                }

                if (operations)
                    Register<OperationRulePack, OperationAllowedRule, OperationDN, bool, OperationDN>("operations", a => a.Resource,
                        Mapping.New<bool>(), "Resource", true);

                if (permissions)
                    Register<PermissionRulePack, PermissionAllowedRule, PermissionDN, bool, PermissionDN>("permissions", a => a.Resource,
                        Mapping.New<bool>(), "Resource", false);

                if (facadeMethods)
                    Register<FacadeMethodRulePack, FacadeMethodAllowedRule, FacadeMethodDN, bool, string>("facadeMethods", a => a.Resource.ToString(),
                        Mapping.New<bool>(), "Resource_Key", false);

                if (entityGroups)
                {
                    Register<EntityGroupRulePack, EntityGroupAllowedRule, EntityGroupDN, EntityGroupAllowedDN, EntityGroupDN>("entityGroups", a => a.Resource,
                        ctx =>new EntityGroupAllowedDN(ParseTypeAllowed(ctx.Parent.Inputs, "In_"), ParseTypeAllowed(ctx.Parent.Inputs, "Out_")), "Resource", false);
                }
            }
        }

        public static TypeAllowed ParseTypeAllowed(IDictionary<string, string> dic, string inOut)
        {
            return TypeAllowedExtensions.Create(
                Mapping.ParseHtmlBool(dic[inOut + "Create"]),
                Mapping.ParseHtmlBool(dic[inOut + "Modify"]),
                Mapping.ParseHtmlBool(dic[inOut + "Read"]),
                Mapping.ParseHtmlBool(dic[inOut + "None"]));
        }

        static void Register<T, AR, R, A, K>(string partialViewName, Func<AR, K> getKey,Mapping<A> allowedMapping,string getKeyRoute, bool embedded)
            where T : BaseRulePack<AR>
            where AR: AllowedRule<R, A>, new()
            where R : IdentifiableEntity

        {
            if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(R)))
                Navigator.AddSetting(new EntitySettings<R>(EntityType.ServerOnly));
            
            string viewPrefix = "~/authAdmin/Views/{0}.cshtml";
            Navigator.AddSetting(new EmbeddedEntitySettings<T>()
            {
                ShowSave = false,
                PartialViewName = e =>  viewPrefix.Formato(partialViewName),
                MappingDefault = new EntityMapping<T>(false)
                    .CreateProperty(m => m.DefaultRule)
                    .SetProperty(m => m.Rules,
                        new MListDictionaryMapping<AR, K>(getKey, getKeyRoute)
                        {
                            ElementMapping = new EntityMapping<AR>(false).SetProperty(p => p.Allowed, allowedMapping)
                        })
            });

            ButtonBarEntityHelper.RegisterEntityButtons<T>((ControllerContext controllerContext, T entity, string viewName, string prefix) =>
                new[] { new ToolBarButton { 
                    OnClick = (embedded ? "postDialog('{0}', '{1}')" :  "SF.submit('{0}', '{1}')").Formato(
                        new UrlHelper(controllerContext.RequestContext).Action((embedded? "save" : "") +  partialViewName, "AuthAdmin"), prefix), 
                    Text = Resources.Save,
                    DivCssClass = ToolBarButton.DefaultEntityDivCssClass 
                } 
                });
        }
    }
}
