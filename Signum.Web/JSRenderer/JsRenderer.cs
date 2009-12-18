using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;

namespace Signum.Web
{
    public interface IJSRenderer
    {
        string ToJS();
    }

    public class JsRenderer
    {
        protected Func<string> renderer;
        public JsRenderer(Func<string> renderer)
        {
            if (renderer == null)
                throw new ArgumentException("renderer"); 
            this.renderer = renderer;
        }

        protected JsRenderer()
        {
        }

        public string ToJS()
        {
            return renderer();
        }

        public static JsRenderer operator & (JsRenderer js1, JsRenderer js2)
        {
            return new JsRenderer(() => js1.ToJS() + ";" + js2.ToJS());
        }
    }

    public static class JSnippetExtensions
    {
        //public static T Extend<T>(this T firt, T second) where T:IJSnippet
        //{
        //    return new JSnippet(() => "$.extend({0},{1})".Formato(firt.ToJS(), second.ToJS())); 
        //}
    }
}
