using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Engine;
using Signum.Entities.Reflection;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.React.Files;
using System.IO;
using Signum.React.Filters;

namespace Signum.React.Authorization
{
    [ValidateModelFilter]
    public class AuthAdminController : ControllerBase
    {
        [HttpGet("api/authAdmin/permissionRules/{roleId}")]
        public PermissionRulePack GetPermissionRules(string roleId)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            var rules = PermissionAuthLogic.GetPermissionRules(Lite.ParsePrimaryKey<RoleEntity>(roleId).FillToString());
            CleanChanges(rules);
            return rules;
        }

        [HttpPost("api/authAdmin/permissionRules")]
        public void SetPermissionRules([Required, FromBody]PermissionRulePack rules)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            PermissionAuthLogic.SetPermissionRules(rules);
        }

        [HttpGet("api/authAdmin/typeRules/{roleId}")]
        public TypeRulePack GetTypeRules(string roleId)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            var rules = TypeAuthLogic.GetTypeRules(Lite.ParsePrimaryKey<RoleEntity>(roleId).FillToString());
            CleanChanges(rules);
            return rules;
        }

        [HttpPost("api/authAdmin/typeRules")]
        public void SetTypeRules([Required, FromBody]TypeRulePack rules)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            TypeAuthLogic.SetTypeRules(rules);
        }



        [HttpGet("api/authAdmin/operationRules/{typeName}/{roleId}")]
        public OperationRulePack GetOperationRules(string typeName, string roleId)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            var rules = OperationAuthLogic.GetOperationRules(Lite.ParsePrimaryKey<RoleEntity>(roleId).FillToString(), TypeLogic.GetType(typeName).ToTypeEntity());
            CleanChanges(rules);
            return rules;
        }

        [HttpPost("api/authAdmin/operationRules")]
        public void SetOperationRules([Required, FromBody]OperationRulePack rules)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            OperationAuthLogic.SetOperationRules(rules);
        }



        [HttpGet("api/authAdmin/propertyRules/{typeName}/{roleId}")]
        public PropertyRulePack GetPropertyRule(string typeName, string roleId)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            var rules = PropertyAuthLogic.GetPropertyRules(Lite.ParsePrimaryKey<RoleEntity>(roleId).FillToString(), TypeLogic.GetType(typeName).ToTypeEntity());
            CleanChanges(rules);
            return rules;
        }

        [HttpPost("api/authAdmin/propertyRules")]
        public void SetPropertyRule([Required, FromBody]PropertyRulePack rules)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            PropertyAuthLogic.SetPropertyRules(rules);
        }



        [HttpGet("api/authAdmin/queryRules/{typeName}/{roleId}")]
        public QueryRulePack GetQueryRules(string typeName, string roleId)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            var rules = QueryAuthLogic.GetQueryRules(Lite.ParsePrimaryKey<RoleEntity>(roleId).FillToString(), TypeLogic.GetType(typeName).ToTypeEntity());
            CleanChanges(rules);
            return rules;
        }

        [HttpPost("api/authAdmin/queryRules")]
        public void SetQueryRules([Required, FromBody]QueryRulePack rules)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            QueryAuthLogic.SetQueryRules(rules);
        }


        [HttpGet("api/authAdmin/downloadAuthRules")]
        public FileStreamResult DowloadAuthRules()
        {
            BasicPermission.AdminRules.AssertAuthorized();

            using (MemoryStream ms = new MemoryStream())
            {
                AuthLogic.ExportRules().Save(ms);

                return FilesController.GetFileStreamResult(new MemoryStream(ms.ToArray()), "AuthRules.xml");
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
