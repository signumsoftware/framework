using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;

namespace Signum.Web
{
    public class ModelStateData
    {
        public ModelStateDictionary ModelState;

        public string NewToStr;
        public string NewtoStrLink;

        public ModelStateData(ModelStateDictionary modelState)
        {
            this.ModelState = modelState;
        }

        public override string ToString()
        {
            return "{{{0}}}".Formato(
                ", ".Combine(
                    "\"jsonResultType\":\"" + JsonResultType.ModelState + "\"",
                    "\"ModelState\":" + this.ModelState.ToJsonData(),
                    NewToStr.TryCC(n => "\"" + EntityBaseKeys.ToStr + "\"" + ": " + n.Quote()),
                    NewtoStrLink.TryCC(n => "\"" + EntityBaseKeys.ToStrLink + "\"" + ": " + n.Quote())));
        }
    }
}
