using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI;
using Signum.Test;
using Signum.Web.Operations;
using Signum.Engine;
using Signum.Entities;
using Signum.Engine.Operations;
using Signum.Test.Extensions;
using Signum.Engine.Basics;

namespace Signum.Web.Extensions.Sample
{
    public class MusicController : Controller
    {
        public ViewResult BandDetail()
        {
            return Navigator.View(this, Database.Retrieve<BandDN>(1), "BandDetail");
        }

        public ViewResult BandRepeater() 
        {
            return Navigator.View(this, Database.Retrieve<BandDN>(1), "BandRepeater");
        }

        public ActionResult CreateAlbumFromBand(string prefix)
        {
            BandDN band = Navigator.ExtractEntity<BandDN>(this);

            AlbumFromBandModel model = new AlbumFromBandModel(band.ToLite());

            ViewData[ViewDataKeys.WriteSFInfo] = true;
            ViewData[ViewDataKeys.OnOk] = JsValidator.EntityIsValid(prefix, new JsOperationExecutor(
                new JsOperationOptions { Prefix = prefix, ControllerUrl = RouteHelper.New().Action("CreateAlbumFromBandOnOk", "Music") })
                .OperationAjax(prefix, JsOpSuccess.DefaultDispatcher)).ToJS();
            
            return Navigator.PopupView(this, model, prefix);
        }

        public JsonResult CreateAlbumFromBandOnOk(string prefix)
        {
            MappingContext<AlbumFromBandModel> context = Navigator.ExtractEntity<AlbumFromBandModel>(this, prefix).ApplyChanges(this.ControllerContext, prefix, true).ValidateGlobal();

            AlbumDN newAlbum = context.Value.Band.ConstructFromLite<AlbumDN>(AlbumOperation.CreateFromBand, new object[] { context.Value.Name, context.Value.Year, context.Value.Label });

            return JsonAction.Redirect(Navigator.ViewRoute(newAlbum));
        }

        public ActionResult CreateGreatestHitsAlbum(List<int> ids, string prefix)
        {
            if (ids == null || ids.Count == 0)
                throw new ArgumentException("You need to specify source albums");

            List<Lite> sourceAlbums = ids.Select(idstr => Lite.Create(typeof(AlbumDN), idstr)).ToList();
            
            IdentifiableEntity entity = OperationLogic.ServiceConstructFromMany(sourceAlbums, typeof(AlbumDN), AlbumOperation.CreateGreatestHitsAlbum);

            return Navigator.View(this, entity);
        }
    }
}
