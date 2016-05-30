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

    public abstract class EntityBase : LineBase
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

        protected virtual Dictionary<string, object> OptionsJSInternal()
        {
            var options = new Dictionary<string, object>
            {
                {"prefix", Prefix }
            };

            if (PartialViewName.HasText() && !Type.IsEmbeddedEntity())
                options.Add("partialViewName", PartialViewName);

            Type type = this.GetElementType();

            if (type.IsEmbeddedEntity())
            {
                if (Implementations != null)
                    throw new ArgumentException("implementations should be null for EmbeddedEntities");

                options.Add("types", new[] { type.ToJsTypeInfo(isSearch: false, prefix: Prefix) });

                PropertyRoute route = this.GetElementRoute();
                options.Add("rootType", Navigator.ResolveWebTypeName(route.RootType));
                options.Add("propertyRoute", route.PropertyString());
            }
            else
            {
                options.Add("types", Implementations.Value.ToJsTypeInfos(isSearch: false, prefix : Prefix));
            }

            if (this.ReadOnly)
                options.Add("isReadOnly", this.ReadOnly);

            if (Create)
                options.Add("create", true);
            if (Remove)
                options.Add("remove", true);
            if (Find)
                options.Add("find", true);
            if (View)
                options.Add("view", true);
            if (Navigate)
                options.Add("navigate", this.Navigate);

            return options;
        }

      

        protected virtual PropertyRoute GetElementRoute()
        {
            return this.PropertyRoute;
        }

        protected virtual Type GetElementType()
        {
            return this.Type;
        }

        public static Type[] ParseTypes(string types)
        {
            if (string.IsNullOrEmpty(types))
                return null;

            return types.Split(',').Select(tn => Navigator.ResolveType(tn)).NotNull().ToArray();
        }

        internal Type CleanRuntimeType
        {
            get
            {
                if (UntypedValue == null)
                    return null;

                return UntypedValue.GetType().IsLite() ? (UntypedValue as Lite<IEntity>).EntityType : UntypedValue.GetType();
            }
        }

        public JsFunction AttachFunction;

        public MvcHtmlString ConstructorScript(JsModule module, string type)
        {
            var result = AttachFunction != null ?
                AttachConstructor(module, type) :
                BasicConstructor(module, type);

            return new MvcHtmlString("<script>" + result + "</script>");
        }

        string AttachConstructor(JsModule module, string type)
        {
            var varModule = JsFunction.VarName(AttachFunction.Module);

            var varLines = JsFunction.VarName(module);

            var line = type.FirstLower();

            var args = AttachFunction.Arguments.ToString(a =>
                a == JsFunction.This ? "that" :
                a == this ? line :
                JsonConvert.SerializeObject(a, AttachFunction.JsonSerializerSettings), ", ");

            var result = "require(['" + module + "', '" + AttachFunction.Module.Name + "'], function(" + varLines + ", " + varModule + ") {\r\n" +
                "var " + line + " = " + NewLine(varLines, type) + ";\r\n" +
                varModule + "." + AttachFunction.FunctionName + "(" + args + ");\r\n" +
                line + ".ready();\r\n" +
                "});";

            if (!AttachFunction.Arguments.Contains(JsFunction.This))
                return result;

            return "(function(that) { " + result + "})(this)";
        }

        string BasicConstructor(JsModule module, string type)
        {
            var varNameLines = JsFunction.VarName(module);

            var result = "require(['" + module + "'], function(" + varNameLines + ") { " + NewLine(varNameLines, type) + ".ready(); });";

            return result;
        }

        string NewLine(string varLines, string type)
        {
            return "new {0}.{1}($('#{2}'), {3})".FormatWith(varLines, type, this.Prefix, JsonConvert.SerializeObject(this.OptionsJSInternal()));
        }
    }
}
