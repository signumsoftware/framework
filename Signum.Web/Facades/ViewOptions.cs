using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Signum.Web
{
    public abstract class ViewOptionsBase
    {
        public bool? ReadOnly { get; set; }

        public EntitySettingsContext Context { get; set; }

        public string PartialViewName { get; set; }

        public TypeContext TypeContext { get; set; }

        internal abstract ViewButtons GetViewButtons();
    }

    public class ViewOkOptions : ViewOptionsBase
    {
        public ViewOkOptions(TypeContext tc)
        {
            TypeContext = tc;
        }

        internal override ViewButtons GetViewButtons()
        {
            return ViewButtons.Ok;
        }
    }

    public class ViewSaveOptions : ViewOptionsBase
    {
        public ViewSaveOptions(TypeContext tc)
        {
            TypeContext = tc;
        }

        internal override ViewButtons GetViewButtons()
        {
            return ViewButtons.Save;
        }
    }

    public enum ViewButtons
    {
        Ok,
        Save
    }
}