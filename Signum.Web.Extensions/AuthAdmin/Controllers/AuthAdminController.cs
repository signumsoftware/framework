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
            if (UserDN.Current != null)
                BasicPermission.AdminRules.Authorize();
        }

        public ViewResult Permissions(Lite<RoleDN> role)
        {
            return Navigator.NormalPage(this, PermissionAuthLogic.GetPermissionRules(role.FillToString()));
        }

        [HttpPost]
        public ActionResult Permissions(FormCollection form)
        {
            Lite<RoleDN> role = this.ExtractLite<RoleDN>("Role");

            var prp = PermissionAuthLogic.GetPermissionRules(role).ApplyChanges(this, ""); ;

            PermissionAuthLogic.SetPermissionRules(prp.Value);

            return RedirectToAction("Permissions", new { role = role.Id });
        }

        public ViewResult Types(Lite<RoleDN> role)
        {
            return Navigator.NormalPage(this, TypeAuthLogic.GetTypeRules(role.FillToString()));
        }

        [HttpPost]
        public ActionResult Types(FormCollection form)
        {
            Lite<RoleDN> role = this.ExtractLite<RoleDN>("Role");

            var prp = TypeAuthLogic.GetTypeRules(role).ApplyChanges(this, ""); ;

            TypeAuthLogic.SetTypeRules(prp.Value);

            return RedirectToAction("Types", new { role = role.Id });
        }

        [HttpPost]
        public ActionResult Properties(Lite<RoleDN> role, Lite<TypeDN> type)
        {
            return this.PopupNavigate(PropertyAuthLogic.GetPropertyRules(role.FillToString(), type.Retrieve()), new PopupNavigateOptions(""));
        }

        [HttpPost]
        public ActionResult SaveProperties(FormCollection form, string prefix)
        {
            Lite<RoleDN> role = this.ExtractLite<RoleDN>(TypeContextUtilities.Compose(prefix, "Role"));
            TypeDN type = this.ExtractEntity<TypeDN>(TypeContextUtilities.Compose(prefix, "Type"));

            var prp = PropertyAuthLogic.GetPropertyRules(role, type).ApplyChanges(this, prefix);

            PropertyAuthLogic.SetPropertyRules(prp.Value);

            if (prp.HasErrors())
                return prp.ToJsonModelState();

            return null;
        }

        [HttpPost]
        public ActionResult Queries(Lite<RoleDN> role, Lite<TypeDN> type)
        {
            return this.PopupNavigate(QueryAuthLogic.GetQueryRules(role.FillToString(), type.Retrieve()), new PopupNavigateOptions(""));
        }

        [HttpPost]
        public JsonNetResult SaveQueries(FormCollection form, string prefix)
        {
            Lite<RoleDN> role = this.ExtractLite<RoleDN>(TypeContextUtilities.Compose(prefix, "Role"));
            TypeDN type = this.ExtractEntity<TypeDN>(TypeContextUtilities.Compose(prefix, "Type"));

            var querys = QueryAuthLogic.GetQueryRules(role, type).ApplyChanges(this, prefix);

            if (querys.HasErrors())
                return querys.ToJsonModelState();

            QueryAuthLogic.SetQueryRules(querys.Value);

            return null;
        }


        [HttpPost]
        public ActionResult Operations(Lite<RoleDN> role, Lite<TypeDN> type)
        {
            return this.PopupNavigate(OperationAuthLogic.GetOperationRules(role.FillToString(), type.Retrieve()), new PopupNavigateOptions(""));
        }

        [HttpPost]
        public ActionResult SaveOperations(FormCollection form, string prefix)
        {
            Lite<RoleDN> role = this.ExtractLite<RoleDN>(TypeContextUtilities.Compose(prefix, "Role"));
            TypeDN type = this.ExtractEntity<TypeDN>(TypeContextUtilities.Compose(prefix, "Type"));

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
