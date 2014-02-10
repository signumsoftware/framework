using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Engine.Chart;

namespace Signum.Web.Chart
{
    public class ColorChartController : Controller
    {
        #region chart color

        public ActionResult Colors(string typeName)
        {
            Type type = Navigator.ResolveType(typeName);

            var model = ChartColorLogic.GetPalette(type);

            return Navigator.NormalPage(this, model);
        }

        public ActionResult SavePalette(string typeName)
        {
            Type type = Navigator.ResolveType(typeName);

            var ctx = ChartColorLogic.GetPalette(type).ApplyChanges(this.ControllerContext, true).ValidateGlobal();

            if (ctx.GlobalErrors.Any())
            {
                this.ModelState.FromContext(ctx);
                return JsonAction.ModelState(ModelState);
            }

            var palette = ctx.Value;

            ChartColorLogic.SavePalette(palette);

            return Redirect(Url.Action<ColorChartController>(cc => cc.Colors(typeName)));
        }

        public ActionResult CreateNewPalette(string typeName)
        {
            Type type = Navigator.ResolveType(typeName);

            ChartColorLogic.CreateNewPalette(type);

            return Redirect(Url.Action<ColorChartController>(cc => cc.Colors(typeName)));
        }

        #endregion 
    }
}