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
        public static JsRenderer SurroundWithFunction(this JsRenderer js)
        {
            return new JsRenderer(() => "function(){ " + js.ToJS() + " }");
        }
    }
}
