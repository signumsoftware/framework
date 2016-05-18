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

namespace Signum.React.Authorization
{
    public class AuthAdminController : ApiController
    {
        [Route("api/authAdmin/permissionRules/{roleId}"), HttpGet]
        public PermissionRulePack GetPermissionRules(string roleId)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            var rules = PermissionAuthLogic.GetPermissionRules(Lite.ParsePrimaryKey<RoleEntity>(roleId).FillToString());
            GraphExplorer.CleanModifications(GraphExplorer.FromRoot(rules));
            return rules;
        }

        [Route("api/authAdmin/permissionRules"), HttpPost]
        public void SetPermissionRules(PermissionRulePack rules)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            PermissionAuthLogic.SetPermissionRules(rules);
        }



        [Route("api/authAdmin/typeRules/{roleId}"), HttpGet]
        public TypeRulePack GetTypeRules(string roleId)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            var rules = TypeAuthLogic.GetTypeRules(Lite.ParsePrimaryKey<RoleEntity>(roleId));
            GraphExplorer.CleanModifications(GraphExplorer.FromRoot(rules));
            return rules;
        }

        [Route("api/authAdmin/typeRules"), HttpPost]
        public void SetTypeRules(TypeRulePack rules)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            TypeAuthLogic.SetTypeRules(rules);
        }



        [Route("api/authAdmin/operationRules/{typeId}/{roleId}"), HttpGet]
        public OperationRulePack GetOperationRules(string typeName, string roleId)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            var rules = OperationAuthLogic.GetOperationRules(Lite.ParsePrimaryKey<RoleEntity>(roleId), TypeLogic.GetType(typeName).ToTypeEntity());
            GraphExplorer.CleanModifications(GraphExplorer.FromRoot(rules));
            return rules;
        }

        [Route("api/authAdmin/operationRules"), HttpPost]
        public void SetOperationRules(OperationRulePack rules)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            OperationAuthLogic.SetOperationRules(rules);
        }



        [Route("api/authAdmin/propertyRules/{typeId}/{roleId}"), HttpGet]
        public PropertyRulePack GetPropertyRule(string typeName, string roleId)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            var rules = PropertyAuthLogic.GetPropertyRules(Lite.ParsePrimaryKey<RoleEntity>(roleId), TypeLogic.GetType(typeName).ToTypeEntity());
            GraphExplorer.CleanModifications(GraphExplorer.FromRoot(rules));
            return rules;
        }

        [Route("api/authAdmin/propertyRules"), HttpPost]
        public void SetPropertyRule(PropertyRulePack rules)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            PropertyAuthLogic.SetPropertyRules(rules);
        }



        [Route("api/authAdmin/queryRules/{typeId}/{roleId}"), HttpGet]
        public QueryRulePack GetQueryRules(string typeName, string roleId)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            var rules = QueryAuthLogic.GetQueryRules(Lite.ParsePrimaryKey<RoleEntity>(roleId), TypeLogic.GetType(typeName).ToTypeEntity());
            GraphExplorer.CleanModifications(GraphExplorer.FromRoot(rules));
            return rules;
        }

        [Route("api/authAdmin/queryRules"), HttpPost]
        public void SetQueryRules(QueryRulePack rules)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            QueryAuthLogic.SetQueryRules(rules);
        }
    }
}