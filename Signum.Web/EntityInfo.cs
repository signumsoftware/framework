using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Entities;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Linq.Expressions;
using Signum.Engine;
using Signum.Web.Controllers;
using Signum.Web.Properties;

namespace Signum.Web
{
    public static class EntityInfoHelper
    {
        public static string HiddenLite(this HtmlHelper helper, string name, Lite lite)
        {
            return helper.Hidden(name, lite.Key()).ToHtmlString();
        }

        public static string HiddenEntityInfo(this HtmlHelper helper, TypeContext tc)
        {
            return helper.HiddenRuntimeInfo(tc) + helper.HiddenStaticInfo(tc);
        }

        public static string HiddenRuntimeInfo(this HtmlHelper helper, TypeContext tc)
        {
            return helper.Hidden(tc.Compose(EntityBaseKeys.RuntimeInfo), new RuntimeInfo(tc.UntypedValue) { Ticks = GetTicks(helper, tc), ForceNewInUI = GetForceNewInUI(helper, tc) }.ToString()).ToHtmlString();
        }

        public static string HiddenStaticInfo(this HtmlHelper helper, TypeContext tc)
        {
            return helper.Hidden(tc.Compose(EntityBaseKeys.StaticInfo), new StaticInfo(tc.UntypedValue.TryCC(uv=>uv.GetType()) ?? tc.Type) { IsReadOnly = tc.ReadOnly }.ToString(), new { disabled = "disabled" }).ToHtmlString();
        }

        public static string HiddenEntityInfo<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property)
        {
            TypeContext<S> typeContext = (TypeContext<S>)Common.WalkExpression(parent, property);
            return helper.HiddenRuntimeInfo(typeContext) + helper.HiddenStaticInfo(typeContext);
        }

        public static string HiddenRuntimeInfo<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property)
        {
            TypeContext<S> typeContext = (TypeContext<S>)Common.WalkExpression(parent, property);
            return helper.HiddenRuntimeInfo(typeContext);
        }

        public static string HiddenStaticInfo<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property)
        {
            TypeContext<S> typeContext = (TypeContext<S>)Common.WalkExpression(parent, property);
            return helper.HiddenStaticInfo(typeContext);
        }

        public static void WriteEntityInfo(this HtmlHelper helper, TypeContext tc)
        {
            helper.Write(helper.HiddenEntityInfo(tc));
        }

        public static void WriteRuntimeInfo<T>(this HtmlHelper helper, TypeContext tc)
        {
            helper.Write(helper.HiddenRuntimeInfo(tc));
        }

        public static void WriteStaticInfo(this HtmlHelper helper, TypeContext tc)
        {
            helper.Write(helper.HiddenStaticInfo(tc));
        }

        public static void WriteEntityInfo<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property)
        {
            helper.Write(helper.HiddenEntityInfo<T, S>(parent, property));
        }

        public static void WriteRuntimeInfo<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property)
        {
            helper.Write(helper.HiddenRuntimeInfo<T, S>(parent, property));
        }

        public static void WriteStaticInfo<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property)
        {
            helper.Write(helper.HiddenStaticInfo<T, S>(parent, property));
        }

        public static long? GetTicks(HtmlHelper helper, TypeContext tc)
        {
            if (tc.ShowTicks && !tc.ReadOnly &&
                (helper.ViewData.ContainsKey(ViewDataKeys.Reactive) || (tc as BaseLine).TryCS(bl => bl.ReloadOnChange) == true))
                return helper.GetChangeTicks(tc.ControlID) ?? 0;
            return null;
        }

        public static bool GetForceNewInUI(HtmlHelper helper, TypeContext tc)
        {
            return helper.ViewData.ContainsKey(ViewDataKeys.ForceNewInUI);
        }
    }

    public class StaticInfo
    {
        public StaticInfo(Type staticType)
        {
            StaticType = staticType;
        }

        public Type StaticType { get; set; }
        public bool IsEmbedded
        {
            get { return typeof(EmbeddedEntity).IsAssignableFrom(StaticType); }
        }
        public bool IsReadOnly { get; set; }

        public override string ToString()
        {
            if (StaticType == null)
                throw new ArgumentException("StaticInfo.StaticType must be set");

            Type cleanStaticType = Reflector.ExtractLite(StaticType) ?? StaticType;

            string staticTypeName = Navigator.GetName(cleanStaticType);

            return "{0};{1};{2}".Formato(
                staticTypeName,
                IsEmbedded ? "e" : "i",
                IsReadOnly ? "r" : ""
                );
        }
    }

    public class RuntimeInfo
    {
        public Type RuntimeType { get; set; }
        public int? IdOrNull { get; set; }
        public bool IsNew { get; set; }
        public long? Ticks { get; set; }
        public bool? ForceNewInUI { get; set; }

        public RuntimeInfo() { }

        public RuntimeInfo(object value)
        {
            if (value == null)
            {
                RuntimeType = null;
                return;
            }

            if (typeof(Lite).IsAssignableFrom(value.GetType()))
            {
                Lite liteValue = value as Lite;
                RuntimeType = liteValue.RuntimeType;
                IdOrNull = liteValue.IdOrNull;
                IsNew = liteValue.IdOrNull == null;
            }
            else if (typeof(EmbeddedEntity).IsAssignableFrom(value.GetType()))
            {
                RuntimeType = value.GetType();
            }
            else if (typeof(IdentifiableEntity).IsAssignableFrom(value.GetType()))
            {
                RuntimeType = value.GetType();
                IIdentifiable identifiable = value as IIdentifiable;
                IdOrNull = identifiable.IdOrNull;
                IsNew = identifiable.IdOrNull == null;
            }
            else
                throw new ArgumentException("Invalid type {0} for RuntimeInfo. It must be Lite, IdentifiableEntity or EmbeddedEntity".Formato(value.GetType()));
        }

        private string RuntimeTypeStr
        {
            get
            {
                if (RuntimeType == null)
                    return "";
                if (typeof(Lite).IsAssignableFrom(RuntimeType))
                    throw new ArgumentException("RuntimeInfo's RuntimeType cannot be of type Lite. Use ExtractLite or construct a RuntimeInfo<T> instead");
                return RuntimeType.Name;
            }
        }

        public override string ToString()
        {
            if (IdOrNull != null && IsNew)
                throw new ArgumentException("Invalid RuntimeInfo parameters: IdOrNull={0} and IsNew=true".Formato(IdOrNull));

            return "{0};{1};{2};{3}".Formato(
                RuntimeTypeStr,
                IdOrNull.TryToString(),
                IsNew || (ForceNewInUI==true && !IdOrNull.HasValue) ? "n" : "o", //2nd condition is for EmbeddedEntities
                Ticks.TryToString()
                );
        }

        public static RuntimeInfo FromFormValue(string formValue)
        {
            string[] parts = formValue.Split(new[] { ";" }, StringSplitOptions.None);
            if (parts.Length != 4)
                throw new ArgumentException("Incorrect sfRuntimeInfo format: {0}".Formato(formValue));

            return new RuntimeInfo
            {
                RuntimeType = parts[0].HasText() ? Navigator.ResolveType(parts[0]) : null,
                IdOrNull = (parts[1].HasText()) ? int.Parse(parts[1]) : (int?)null,
                IsNew = parts[2]=="n" ? true : false,
                Ticks = (parts[3].HasText()) ? long.Parse(parts[3]) : (long?)null
            };
        }
    }
}
