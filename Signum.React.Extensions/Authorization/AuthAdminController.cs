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

namespace Signum.React.Authorization
{
    public class AuthAdminController : ApiController
    {
        [Route("api/authAdmin/permissionRules/{roleId}"), HttpGet]
        public PermissionRulePack GetPermissionRules(string roleId)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            return PermissionAuthLogic.GetPermissionRules(Lite.ParsePrimaryKey<RoleEntity>(roleId));
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
            return TypeAuthLogic.GetTypeRules(Lite.ParsePrimaryKey<RoleEntity>(roleId));
        }


        [Route("api/authAdmin/typeRules"), HttpPost]
        public void SetTypeRules(TypeRulePack rules)
        {
            BasicPermission.AdminRules.AssertAuthorized();
            TypeAuthLogic.SetTypeRules(rules);
        }
    }
}