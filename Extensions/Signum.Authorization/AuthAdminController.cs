using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Signum.API.Filters;
using Signum.Authorization.Rules;

namespace Signum.Authorization;

[ValidateModelFilter]
public class AuthAdminController : ControllerBase
{
    [HttpGet("api/authAdmin/permissionRules/{roleId}")]
    public PermissionRulePack GetPermissionRules(string roleId)
    {
        BasicPermission.AdminRules.AssertAuthorized();
        var rules = PermissionAuthLogic.GetPermissionRules(Lite.ParsePrimaryKey<RoleEntity>(roleId).FillLiteModel());
        CleanChanges(rules);
        return rules;
    }

    [HttpPost("api/authAdmin/permissionRules")]
    public void SetPermissionRules([Required, FromBody] PermissionRulePack rules)
    {
        BasicPermission.AdminRules.AssertAuthorized();
        PermissionAuthLogic.SetPermissionRules(rules);
        Schema.Current.InvalidateMetadata();
    }

    [HttpGet("api/authAdmin/typeRules/{roleId}")]
    public TypeRulePack GetTypeRules(string roleId)
    {
        BasicPermission.AdminRules.AssertAuthorized();
        var rules = TypeAuthLogic.GetTypeRules(Lite.ParsePrimaryKey<RoleEntity>(roleId).FillLiteModel());
        CleanChanges(rules);
        return rules;
    }

    [HttpPost("api/authAdmin/typeRules")]
    public void SetTypeRules([Required, FromBody] TypeRulePack rules)
    {
        BasicPermission.AdminRules.AssertAuthorized();
        TypeAuthLogic.SetTypeRules(rules);
    }



    [HttpGet("api/authAdmin/operationRules/{typeName}/{roleId}")]
    public OperationRulePack GetOperationRules(string typeName, string roleId)
    {
        BasicPermission.AdminRules.AssertAuthorized();
        var rules = OperationAuthLogic.GetOperationRules(Lite.ParsePrimaryKey<RoleEntity>(roleId).FillLiteModel(), TypeLogic.GetType(typeName).ToTypeEntity());
        CleanChanges(rules);
        return rules;
    }

    [HttpPost("api/authAdmin/operationRules")]
    public void SetOperationRules([Required, FromBody] OperationRulePack rules)
    {
        BasicPermission.AdminRules.AssertAuthorized();
        OperationAuthLogic.SetOperationRules(rules);
        Schema.Current.InvalidateMetadata();
    }



    [HttpGet("api/authAdmin/propertyRules/{typeName}/{roleId}")]
    public PropertyRulePack GetPropertyRule(string typeName, string roleId)
    {
        BasicPermission.AdminRules.AssertAuthorized();
        var rules = PropertyAuthLogic.GetPropertyRules(Lite.ParsePrimaryKey<RoleEntity>(roleId).FillLiteModel(), TypeLogic.GetType(typeName).ToTypeEntity());
        CleanChanges(rules);
        return rules;
    }

    [HttpPost("api/authAdmin/propertyRules")]
    public void SetPropertyRule([Required, FromBody] PropertyRulePack rules)
    {
        BasicPermission.AdminRules.AssertAuthorized();
        PropertyAuthLogic.SetPropertyRules(rules);
        Schema.Current.InvalidateMetadata();
    }



    [HttpGet("api/authAdmin/queryRules/{typeName}/{roleId}")]
    public QueryRulePack GetQueryRules(string typeName, string roleId)
    {
        BasicPermission.AdminRules.AssertAuthorized();
        var rules = QueryAuthLogic.GetQueryRules(Lite.ParsePrimaryKey<RoleEntity>(roleId).FillLiteModel(), TypeLogic.GetType(typeName).ToTypeEntity());
        CleanChanges(rules);
        return rules;
    }

    [HttpPost("api/authAdmin/queryRules")]
    public void SetQueryRules([Required, FromBody] QueryRulePack rules)
    {
        BasicPermission.AdminRules.AssertAuthorized();
        QueryAuthLogic.SetQueryRules(rules);
        Schema.Current.InvalidateMetadata();
    }


    [HttpGet("api/authAdmin/downloadAuthRules")]
    public FileStreamResult DowloadAuthRules()
    {
        BasicPermission.AdminRules.AssertAuthorized();

        using (MemoryStream ms = new MemoryStream())
        {
            AuthLogic.ExportRules().Save(ms);

            return MimeMapping.GetFileStreamResult(new MemoryStream(ms.ToArray()), "AuthRules.xml");
        }
    }

    [HttpPost("api/authAdmin/trivialMergeRole")]
    public Lite<RoleEntity> TrivialMergeRole([FromBody] List<Lite<RoleEntity>> roles)
    {
        //BasicPermission.AdminRules.AssertAuthorized();

        return AuthLogic.GetOrCreateTrivialMergeRole(roles);
    }

    private static void CleanChanges(ModelEntity rules)
    {
        var graph = GraphExplorer.FromRoot(rules);
        var conditions = graph.OfType<WithConditionsModel<TypeAllowed>>().SelectMany(a => a.ConditionRules).ToList();
        conditions.ForEach(con => graph.UnionWith(GraphExplorer.FromRoot(con)));
        GraphExplorer.CleanModifications(graph);
        GraphExplorer.SetDummyRowIds(graph);
    }
}
