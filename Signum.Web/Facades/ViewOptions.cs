using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities;

namespace Signum.Web
{
    public class NavigateOptions
    {
        public NavigateOptions(IRootEntity entity)
        {
            Entity = entity;
        }

        public bool? ReadOnly { get; set; }

        public string PartialViewName { get; set; }

        public IRootEntity Entity { get; set; }
    }

    public abstract class PopupOptionsBase
    {
        public bool? ReadOnly { get; set; }

        public string PartialViewName { get; set; }
    
        public TypeContext TypeContext { get; set; }

        public abstract ViewButtons ViewButtons { get; }
    }

    public class PopupViewOptions : PopupOptionsBase
    {
        public PopupViewOptions(TypeContext tc)
        {
            TypeContext = tc;
        }

        public override ViewButtons ViewButtons 
        {
            get { return Web.ViewButtons.Ok; } 
        }
    }

    public class PopupNavigateOptions : PopupOptionsBase
    {
        public PopupNavigateOptions(TypeContext tc)
        {
            TypeContext = tc;
        }

        public override ViewButtons ViewButtons
        {
            get { return Web.ViewButtons.Save; }
        }
    }

    public enum ViewButtons
    {
        Ok,
        Save
    }
}