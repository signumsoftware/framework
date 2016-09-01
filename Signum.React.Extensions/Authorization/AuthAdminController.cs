using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.React.Facades;
using Signum.React.Authorization;
using Signum.Engine;
using Signum.Entities.Reflection;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.React.Files;
using System.IO;
using Signum.React.Filters;

namespace Signum.React.Authorization
{
    public class AuthAdminController : ApiController
    {
        [Route("api/authAdmin/permissionRules/{roleId}"), HttpGet]
        public PermissionRulePack GetPermissionRules(string roleId)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            var rules = PermissionAuthLogic.GetPermissionRules(Lite.ParsePrimaryKey<RoleEntity>(roleId).FillToString());
            CleanChanges(rules);
            return rules;
        }

        [Route("api/authAdmin/permissionRules"), HttpPost, ValidateModelFilter]
        public void SetPermissionRules(PermissionRulePack rules)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            PermissionAuthLogic.SetPermissionRules(rules);
        }



        [Route("api/authAdmin/typeRules/{roleId}"), HttpGet]
        public TypeRulePack GetTypeRules(string roleId)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            var rules = TypeAuthLogic.GetTypeRules(Lite.ParsePrimaryKey<RoleEntity>(roleId).FillToString());
            CleanChanges(rules);
            return rules;
        }

        [Route("api/authAdmin/typeRules"), HttpPost, ValidateModelFilter]
        public void SetTypeRules(TypeRulePack rules)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            TypeAuthLogic.SetTypeRules(rules);
        }



        [Route("api/authAdmin/operationRules/{typeName}/{roleId}"), HttpGet]
        public OperationRulePack GetOperationRules(string typeName, string roleId)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            var rules = OperationAuthLogic.GetOperationRules(Lite.ParsePrimaryKey<RoleEntity>(roleId).FillToString(), TypeLogic.GetType(typeName).ToTypeEntity());
            CleanChanges(rules);
            return rules;
        }

        [Route("api/authAdmin/operationRules"), HttpPost, ValidateModelFilter]
        public void SetOperationRules(OperationRulePack rules)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            OperationAuthLogic.SetOperationRules(rules);
        }



        [Route("api/authAdmin/propertyRules/{typeName}/{roleId}"), HttpGet]
        public PropertyRulePack GetPropertyRule(string typeName, string roleId)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            var rules = PropertyAuthLogic.GetPropertyRules(Lite.ParsePrimaryKey<RoleEntity>(roleId).FillToString(), TypeLogic.GetType(typeName).ToTypeEntity());
            CleanChanges(rules);
            return rules;
        }

        [Route("api/authAdmin/propertyRules"), HttpPost, ValidateModelFilter]
        public void SetPropertyRule(PropertyRulePack rules)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            PropertyAuthLogic.SetPropertyRules(rules);
        }



        [Route("api/authAdmin/queryRules/{typeName}/{roleId}"), HttpGet]
        public QueryRulePack GetQueryRules(string typeName, string roleId)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            var rules = QueryAuthLogic.GetQueryRules(Lite.ParsePrimaryKey<RoleEntity>(roleId).FillToString(), TypeLogic.GetType(typeName).ToTypeEntity());
            CleanChanges(rules);
            return rules;
        }

        [Route("api/authAdmin/queryRules"), HttpPost, ValidateModelFilter]
        public void SetQueryRules(QueryRulePack rules)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            QueryAuthLogic.SetQueryRules(rules);
        }


        [Route("api/authAdmin/downloadAuthRules"), HttpGet]
        public HttpResponseMessage DowloadAuthRules()
        {
            BasicPermission.AdminRules.AssertAuthorized();

            using (MemoryStream ms = new MemoryStream())
            {
                AuthLogic.ExportRules().Save(ms);

                return FilesController.GetHttpReponseMessage(new MemoryStream(ms.ToArray()), "AuthRules.xml");
            }
        }
        
        private static void CleanChanges(ModelEntity rules)
        {
            var graph = GraphExplorer.FromRoot(rules);
            var conditions = graph.OfType<TypeAllowedAndConditions>().SelectMany(a => a.Conditions).ToList();
            conditions.ForEach(con => graph.UnionWith(GraphExplorer.FromRoot(con)));
            GraphExplorer.CleanModifications(graph);
            GraphExplorer.SetDummyRowIds(graph);
        }
    }
}