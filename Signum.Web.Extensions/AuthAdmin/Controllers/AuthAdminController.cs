using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Threading;
using Signum.Entities.Authorization;
using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Services;
using Signum.Utilities;
using System.Net.Mail;
using System.Net;
using System.Text;
using Signum.Entities;
using Signum.Web.Controllers;
using System.IO;
using System.Xml;
using Signum.Entities.Basics;
using System.Linq;

namespace Signum.Web.AuthAdmin
{
    public class AuthAdminController : Controller
    {
        protected override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (UserEntity.Current != null)
                BasicPermission.AdminRules.AssertAuthorized();
        }

        public ViewResult Permissions(Lite<RoleEntity> role)
        {
            return Navigator.NormalPage(this, PermissionAuthLogic.GetPermissionRules(role.FillToString()));
        }

        [HttpPost]
        public ActionResult Permissions(FormCollection form)
        {
            Lite<RoleEntity> role = this.ExtractLite<RoleEntity>("Role");

            var prp = PermissionAuthLogic.GetPermissionRules(role).ApplyChanges(this, ""); ;

            PermissionAuthLogic.SetPermissionRules(prp.Value);

            return RedirectToAction("Permissions", new { role = role.Id });
        }

        public ViewResult Types(Lite<RoleEntity> role)
        {
            return Navigator.NormalPage(this, TypeAuthLogic.GetTypeRules(role.FillToString()));
        }

        [HttpPost]
        public ActionResult Types(FormCollection form)
        {
            Lite<RoleEntity> role = this.ExtractLite<RoleEntity>("Role");

            var prp = TypeAuthLogic.GetTypeRules(role).ApplyChanges(this, ""); ;

            TypeAuthLogic.SetTypeRules(prp.Value);

            return RedirectToAction("Types", new { role = role.Id });
        }

        [HttpPost]
        public ActionResult Properties(Lite<RoleEntity> role, Lite<TypeEntity> type)
        {
            return this.PopupNavigate(PropertyAuthLogic.GetPropertyRules(role.FillToString(), type.Retrieve()));
        }

        [HttpPost]
        public ActionResult SaveProperties(FormCollection form, string prefix)
        {
            Lite<RoleEntity> role = this.ExtractLite<RoleEntity>(TypeContextUtilities.Compose(prefix, "Role"));
            TypeEntity type = this.ExtractEntity<TypeEntity>(TypeContextUtilities.Compose(prefix, "Type"));

            var prp = PropertyAuthLogic.GetPropertyRules(role, type).ApplyChanges(this, prefix);

            PropertyAuthLogic.SetPropertyRules(prp.Value);

            if (prp.HasErrors())
                return prp.ToJsonModelState();

            return null;
        }

        [HttpPost]
        public ActionResult Queries(Lite<RoleEntity> role, Lite<TypeEntity> type)
        {
            return this.PopupNavigate(QueryAuthLogic.GetQueryRules(role.FillToString(), type.Retrieve()));
        }

        [HttpPost]
        public JsonNetResult SaveQueries(FormCollection form, string prefix)
        {
            Lite<RoleEntity> role = this.ExtractLite<RoleEntity>(TypeContextUtilities.Compose(prefix, "Role"));
            TypeEntity type = this.ExtractEntity<TypeEntity>(TypeContextUtilities.Compose(prefix, "Type"));

            var querys = QueryAuthLogic.GetQueryRules(role, type).ApplyChanges(this, prefix);

            if (querys.HasErrors())
                return querys.ToJsonModelState();

            QueryAuthLogic.SetQueryRules(querys.Value);

            return null;
        }


        [HttpPost]
        public ActionResult Operations(Lite<RoleEntity> role, Lite<TypeEntity> type)
        {
            return this.PopupNavigate(OperationAuthLogic.GetOperationRules(role.FillToString(), type.Retrieve()));
        }

        [HttpPost]
        public ActionResult SaveOperations(FormCollection form, string prefix)
        {
            Lite<RoleEntity> role = this.ExtractLite<RoleEntity>(TypeContextUtilities.Compose(prefix, "Role"));
            TypeEntity type = this.ExtractEntity<TypeEntity>(TypeContextUtilities.Compose(prefix, "Type"));

            var opers = OperationAuthLogic.GetOperationRules(role, type).ApplyChanges(this, prefix);

            if (opers.HasErrors())
                return opers.ToJsonModelState();

            OperationAuthLogic.SetOperationRules(opers.Value);

            return null;
        }

        [HttpGet]
        public FileResult Export()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                AuthLogic.ExportRules().Save(ms);

                return File(ms.ToArray(), MimeType.FromExtension("xml"), "AuthRules.xml");
            }
        }
    }
}
