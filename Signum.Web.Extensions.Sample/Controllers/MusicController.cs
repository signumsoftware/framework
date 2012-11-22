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
using Signum.Utilities;

namespace Signum.Web.Extensions.Sample
{
    public class MusicController : Controller
    {
        public ViewResult BandDetail()
        {
            return Navigator.NormalPage(this, new NavigateOptions(Database.Retrieve<BandDN>(1)) { PartialViewName = "BandDetail" });
        }

        public ViewResult BandRepeater() 
        {
            return Navigator.NormalPage(this, new NavigateOptions(Database.Retrieve<BandDN>(1)) { PartialViewName = "BandRepeater" });
        }

        [HttpPost]
        public ActionResult CreateAlbumFromBand(string prefix)
        {
            BandDN band = Navigator.ExtractEntity<BandDN>(this);

            AlbumFromBandModel model = new AlbumFromBandModel(band.ToLite());

            ViewData[ViewDataKeys.OnOk] = new JsOperationExecutor(new JsOperationOptions 
            { 
                Prefix = prefix,
                ControllerUrl = Url.Action<MusicController>(mc => mc.CreateAlbumFromBandExecute(prefix))
            }).validateAndAjax().ToJS();

            TypeContext tc = TypeContextUtilities.UntypedNew(model, prefix);
            return this.PopupOpen(new PopupViewOptions(tc));
        }

        [HttpPost]
        public JsonResult CreateAlbumFromBandExecute(string prefix)
        {
            var modelo = Navigator.ExtractEntity<AlbumFromBandModel>(this, prefix)
                .ApplyChanges(this.ControllerContext, prefix, true).Value;

            AlbumDN newAlbum = modelo.Band.ConstructFromLite<AlbumDN>(AlbumOperation.CreateFromBand, 
                new object[] { modelo.Name, modelo.Year, modelo.Label });

            return JsonAction.Redirect(Navigator.NavigateRoute(newAlbum));
        }

        [HttpPost]
        public ActionResult CreateGreatestHitsAlbum()
        {
            var sourceAlbums = Navigator.ParseLiteKeys<AlbumDN>(Request["keys"]);
            
            var newAlbum = OperationLogic.ConstructFromMany<AlbumDN, AlbumDN>(sourceAlbums, AlbumOperation.CreateGreatestHitsAlbum);

            return Navigator.NormalPage(this, newAlbum);
        }

        /// <summary>
        /// Button that tests opening a popup and on Ok => submit form + popup
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult CloneWithData(string prefix)
        {
            ViewData[ViewDataKeys.OnOk] = JsValidator.EntityIsValid(prefix,
                                    Js.Submit(Url.Action<MusicController>(mc => mc.Clone(prefix)),
                                        Js.SerializePopupFunction(prefix))
                                    ).ToJS();

            ViewData[ViewDataKeys.Title] = "Introduzca el nombre del álbum";

            var model = new ValueLineBoxModel(this.ExtractEntity<AlbumDN>(), ValueLineBoxType.String, "Name", "Write new album's name");
            return this.PopupOpen(new PopupViewOptions(new TypeContext<ValueLineBoxModel>(model, prefix)));
        }

        [HttpPost]
        public ActionResult Clone(string prefix)
        {
            var modelo = this.ExtractEntity<ValueLineBoxModel>(prefix)
                               .ApplyChanges(this.ControllerContext, prefix, false).Value;

            var album = this.ExtractLite<AlbumDN>(null);

            AlbumDN newAlbum = album.ConstructFromLite<AlbumDN>(AlbumOperation.Clone);
            newAlbum.Name = modelo.StringValue;

            return Navigator.NormalPage(this, newAlbum);
        }
    }
}
