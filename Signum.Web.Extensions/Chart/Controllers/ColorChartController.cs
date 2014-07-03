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

            var ctx = ChartColorLogic.GetPalette(type).ApplyChanges(this).ValidateGlobal();

            if (ctx.HasErrors())
                return ctx.ToJsonModelState();

            var palette = ctx.Value;

            ChartColorLogic.SavePalette(palette);

            return Navigator.NormalControl(this, palette);
        }

        public ActionResult CreateNewPalette(string typeName)
        {
            Type type = Navigator.ResolveType(typeName);

            ChartColorLogic.CreateNewPalette(type, Request["palette"]);

            var model = ChartColorLogic.GetPalette(type);

            return Navigator.NormalControl(this, model);
        }

        #endregion 
    
        public ActionResult DeletePalette(string typeName)
        {
            Type type = Navigator.ResolveType(typeName);

            ChartColorLogic.DeletePalette(type);

            var model = ChartColorLogic.GetPalette(type);

            return Navigator.NormalControl(this, model);
        }
    }
}