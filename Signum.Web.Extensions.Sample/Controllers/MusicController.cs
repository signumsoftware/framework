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

        public ActionResult CreateAlbumFromBand(string prefix)
        {
            BandDN band = Navigator.ExtractEntity<BandDN>(this);

            AlbumFromBandModel model = new AlbumFromBandModel(band.ToLite());

            ViewData[ViewDataKeys.WriteSFInfo] = true;
            ViewData[ViewDataKeys.OnSave] = new JsOperationExecutor(new JsOperationOptions 
                { 
                    Prefix = prefix,
                    ControllerUrl = RouteHelper.New().Action("CreateAlbumFromBandOnSave", "Music") 
                }).validateAndAjax().ToJS();

            TypeContext tc = TypeContextUtilities.UntypedNew(model, prefix);
            return this.PopupOpen(new PopupNavigateOptions(tc));
        }

        public JsonResult CreateAlbumFromBandOnSave(string prefix)
        {
            MappingContext<AlbumFromBandModel> context = Navigator.ExtractEntity<AlbumFromBandModel>(this, prefix).ApplyChanges(this.ControllerContext, prefix, true).ValidateGlobal();

            AlbumDN newAlbum = context.Value.Band.ConstructFromLite<AlbumDN>(AlbumOperation.CreateFromBand, new object[] { context.Value.Name, context.Value.Year, context.Value.Label });

            return JsonAction.Redirect(Navigator.NavigateRoute(newAlbum));
        }

        public ActionResult CreateGreatestHitsAlbum(List<int> ids, string prefix)
        {
            if (ids == null || ids.Count == 0)
                throw new ArgumentException("You need to specify source albums");

            List<Lite> sourceAlbums = ids.Select(idstr => Lite.Create(typeof(AlbumDN), idstr)).ToList();
            
            IdentifiableEntity entity = OperationLogic.ServiceConstructFromMany(sourceAlbums, typeof(AlbumDN), AlbumOperation.CreateGreatestHitsAlbum);

            return Navigator.NormalPage(this, entity);
        }

        /// <summary>
        /// Button that tests opening a popup and on Ok => submit form + popup
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public ActionResult CloneWithData(string prefix)
        {
            ViewData[ViewDataKeys.OnOk] = JsValidator.EntityIsValid(prefix,
                                    Js.Submit(
                                        RouteHelper.New().Action("Clone", "Music"),
                                        "function() {{ var data = {{ prefix:'{0}' }}; return $.extend(data, SF.Popup.serializeJson('{0}')); }}".Formato(prefix))
                                    ).ToJS();

            ViewData[ViewDataKeys.Title] = "Introduzca los datos de las disponibilidades a crear";

            ViewData[ViewDataKeys.WriteSFInfo] = true;

            TypeContext tc = TypeContextUtilities.UntypedNew(new ValueLineBoxModel(this.ExtractEntity<AlbumDN>(), ValueLineBoxType.String, "Name", "Write new album's name"), prefix);
            return this.PopupOpen(new PopupViewOptions(tc));
        }

        public ActionResult Clone(string prefix)
        {
            var valueCtx = this.ExtractEntity<ValueLineBoxModel>(prefix)
                               .ApplyChanges(this.ControllerContext, prefix, false)
                               .ValidateGlobal();
            var album = this.ExtractLite<AlbumDN>(null);

            if (valueCtx.GlobalErrors.Any())
            {
                this.ModelState.FromContext(valueCtx);
                return JsonAction.ModelState(ModelState);
            }

            AlbumDN newAlbum = album.ConstructFromLite<AlbumDN>(AlbumOperation.Clone);
            newAlbum.Name = valueCtx.Value.StringValue;

            return Navigator.NormalPage(this, newAlbum);
        }
    }
}
