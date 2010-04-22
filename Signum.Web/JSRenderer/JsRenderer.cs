using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;

namespace Signum.Web
{
    public abstract class JsRenderer
    {
        public Func<string> Renderer { get; set; }
        
        protected JsRenderer()
        {
        }

        public string ToJS()
        {
            return Renderer();
        }
    }

    public class JsInstruction :JsRenderer
    {
        protected JsInstruction()
        {
        }

         public JsInstruction(Func<string> renderer)
        {
            if (renderer == null)
                throw new ArgumentException("renderer");
            this.Renderer = renderer;
        }

        public static JsInstruction operator & (JsInstruction js1, JsInstruction js2)
        {
            return new JsInstruction(() => js1.ToJS() + "; " + js2.ToJS());
        }

        public static implicit operator JsInstruction(string code)
        {
            return new JsInstruction(() => code); 
        }
    }

    public class JsValue<T> : JsRenderer
    {
        public JsValue(string code)
        {
            Renderer = () => code;
        }

        protected JsValue() { }

        public static implicit operator JsValue<T>(T value)
        {
            object obj = (object)value;
            if (obj == null)
                return new JsValue<T>() { Renderer = () => "null" };
            if (obj is bool)
                return new JsValue<T>() { Renderer = () => ((bool)obj) ? "true" : "false" };
            else if (obj is string)
                return new JsValue<T>() { Renderer = () => ((string)obj).Quote() };
            else
                return new JsValue<T>() { Renderer = () => value.ToString() }; //numbers an other
        }
    }
}
