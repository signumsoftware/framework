
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Engine;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.Entities.Authorization;
using Signum.Engine.Operations;
using Signum.Engine.Authorization;
using Signum.Engine.UserAssets;
using Signum.Entities.UserAssets;
using Signum.Web.UserAssets;


namespace Signum.Web.UserAssets
{
    public class UserAssetController : Controller
    {
        public ActionResult Import()
        {
            return View(UserAssetsClient.ViewPrefix.FormatWith("ImportUserAsset")); 
        }

        [HttpPost]
        public ActionResult ImportFile()
        {
            HttpPostedFileBase hpf = Request.Files[Request.Files.Cast<string>().Single()];

            var bytes = hpf.InputStream.ReadAllBytes();

            var model = UserAssetsImporter.Preview(bytes);

            ViewData["Document"] = Convert.ToBase64String(bytes);

            return View(UserAssetsClient.ViewPrefix.FormatWith("ImportUserAsset"), model);  

        }

        static Mapping<UserAssetPreviewModel> mapping = new EntityMapping<UserAssetPreviewModel>(false)
            .SetProperty(m=>m.Lines, new MListDictionaryMapping<UserAssetPreviewLine, Guid>(a=>a.Guid)
                .SetElementMapping(new EntityMapping<UserAssetPreviewLine>(false).CreateProperty(a=>a.OverrideEntity)));

        [HttpPost]
        public ActionResult ImportConfirm()
        {
            var bytes = Convert.FromBase64String(Request.Form["Document"]);

            var preview = UserAssetsImporter.Preview(bytes);

            preview.ApplyChanges(this, mapping, "");

            UserAssetsImporter.Import(bytes, preview);

            ViewData["Message"] = UserAssetMessage.SucessfullyImported.NiceToString();

            return View(UserAssetsClient.ViewPrefix.FormatWith("ImportUserAsset"));
        }

        public FileResult Export(Lite<IUserAssetEntity> entity)
        {
            var result = UserAssetsExporter.ToXml(entity.Retrieve());

            return File(result, MimeType.FromExtension("xml"), "{0}{1}.xml".FormatWith(entity.EntityType.Name, entity.Id));
        }
    }
}
