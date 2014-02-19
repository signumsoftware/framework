using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.Reflection;
using System.Web.Script.Serialization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Web;

namespace Signum.Web
{
    public static class EntityBaseKeys
    {
        public const string EntityState = "sfEntityState";
        public const string RuntimeInfo = "sfRuntimeInfo";
        public const string StaticInfo = "sfStaticInfo";
        public const string Entity = "sfEntity";
        public const string Template = "sfTemplate";
        public const string ToStr = "sfToStr";
        public const string Link = "sfLink";
        public const string IsNew = "sfIsNew";
        public const string Detail = "sfDetail";
    }

    public abstract class EntityBase : BaseLine
    {
        public EntityBase(Type type, object untypedValue, Context parent, string prefix, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, prefix, propertyRoute)
        {
            View = true;
            Create = true;
            Find = true;
            Remove = true;
        }

        public bool HasManyImplementations
        {
            get 
            {
                return Implementations != null && !Implementations.Value.IsByAll && Implementations.Value.Types.Count() > 1;
            }
        }

        public Implementations? Implementations { get; set; }

        public bool View { get; set; }
        public bool Navigate { get; set; }
        public bool Create { get; set; }
        public bool Find { get; set; }
        public bool Remove { get; set; }
        public bool ReadOnlyEntity { get; set; }

        bool preserveViewData = false; 
        /// <summary>
        /// When rendering the line content, it will preserve the ViewData values except the Model
        /// </summary>
        public bool PreserveViewData
        {
            get { return preserveViewData; }
            set { preserveViewData = value; }
        }

        public string PartialViewName { get; set; }

        public virtual string SFControlThen(string functionCall)
        {
            return JsFunction.SFControlThen(Prefix, functionCall);
        }

        protected virtual JObject OptionsJSInternal()
        {
            JObject options = new JObject
            {
                {"prefix", Prefix }
            }; 

            if (PartialViewName.HasText() && !Type.IsEmbeddedEntity())
                options.Add("partialViewName", PartialViewName);

            return options;
        }

        internal Type CleanRuntimeType 
        { 
            get 
            {
                if (UntypedValue == null)
                    return null;

                return UntypedValue.GetType().IsLite() ? (UntypedValue as Lite<IIdentifiable>).EntityType : UntypedValue.GetType();
            }
        }

        internal bool? IsNew
        {
            get 
            {
                return (UntypedValue as IIdentifiable).TryCS(i => i.IsNew) ??
                       (UntypedValue as Lite<IIdentifiable>).TryCS(l => l.IsNew);
            }
        }

        internal int? IdOrNull
        {
            get
            {
                return (UntypedValue as IIdentifiable).TryCS(i => i.IdOrNull) ??
                       (UntypedValue as Lite<IIdentifiable>).TryCS(l => l.IdOrNull);
            }
        }

        internal string ToStr
        {
            get 
            {
                return (UntypedValue as IIdentifiable).TryCC(i => i.ToString()) ??
                       (UntypedValue as Lite<IIdentifiable>).TryCC(l => l.ToString());
            }
        }

        public JsLineFunction AttachFunction;

        public MvcHtmlString ConstructorScript(string module, string type)
        {
            var info = new JsLineFunction.LineInfo
            {
                Module = module,
                Type = type,
                Prefix = Prefix,
                Options = OptionsJSInternal()
            };

            var result = AttachFunction != null ? AttachFunction.SetOptions(info).ToString() :
                JsLineFunction.BasicConstructor(info);

            return new MvcHtmlString("<script>" + result + "</script>");
        }
    }


    public class JsLineFunction : JsFunction
    {

        /// <summary>
        /// require("Signum/Lines", "module", function(Lines, mod) { mod.functionName(new Lines.EntityLine($("#Prefix")), arguments...); });
        /// </summary>
        public JsLineFunction(string module, string functionName, params object[] arguments) :
            base(module, functionName, arguments)
        {
        }

        LineInfo lineInfo; 
        internal JsLineFunction SetOptions(LineInfo lineInfo)
        {
            this.lineInfo = lineInfo;
            return this;
        }

        public override string ToString()
        {
            var varModule = VarName(Module);

            var varLines = VarName(lineInfo.Module);

            var args = string.IsNullOrEmpty(Arguments) ? null : (", " + Arguments);

            return "require(['" + lineInfo.Module + "', '" + Module + "'], function(" + varLines + ", " + varModule + ") {\r\n" +
                "var " + lineInfo.Type + " = " + NewLine(varLines, lineInfo) + ";\r\n" +
                varModule + "." + FunctionName + "(" + lineInfo.Type + args + ");\r\n" +
                lineInfo.Type + ".ready();\r\n" +
                "});";
        }

        public static string BasicConstructor(LineInfo lineInfo)
        {
            var varNameLines = VarName(lineInfo.Module);

            var result = "require(['" + lineInfo.Module + "'], function(" + varNameLines + ") { " + NewLine(varNameLines, lineInfo) + ".ready(); });";

            return result; 
        }

        static string NewLine(string varLines, LineInfo lineInfo)
        {
            return "new {0}.{1}($('#{2}'), {3})".Formato(varLines, lineInfo.Type, lineInfo.Prefix, lineInfo.Options.ToString());
        }

        public class LineInfo
        {
            public string Module; 
            public string Type;
            public string Prefix;
            public JObject Options;
        }
    }
   
}

//require("Lines", function(Lines) { new EntityLine($("#myLine")); });
//require("Lines", "Blas" , function(Blas, Lines) { Blas.Attach(new EntityLine($("#myLine"))); });
//require("Lines", "Blas" , function(Blas, Lines) { Blas.Attach(new EntityLine($("#myLine"), {num : "hola"})); });

//require("Operations", function(Operations) { Operations.defaultClick({}); });
