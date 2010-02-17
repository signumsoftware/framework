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

namespace Signum.Web
{
    public static class EntityInfoHelper
    {
        public static string HiddenEntityInfo(this HtmlHelper helper, string prefix, RuntimeInfo runtimeInfo, StaticInfo staticInfo)
        {
            return helper.HiddenRuntimeInfo(prefix, runtimeInfo) + helper.HiddenStaticInfo(prefix, staticInfo);
        }

        public static string HiddenRuntimeInfo(this HtmlHelper helper, string prefix, RuntimeInfo runtimeInfo)
        {
            return helper.Hidden(TypeContext.Compose(prefix, EntityBaseKeys.RuntimeInfo), runtimeInfo.ToString());
        }

        public static string HiddenStaticInfo(this HtmlHelper helper, string prefix, StaticInfo staticInfo)
        {
            return helper.Hidden(TypeContext.Compose(prefix, EntityBaseKeys.StaticInfo), staticInfo.ToString(), new {disabled = "disabled"});
        }

        public static string HiddenEntityInfo<T>(this HtmlHelper helper, TypeContext<T> parent)
        {
            return helper.HiddenRuntimeInfo<T>(parent) + helper.HiddenStaticInfo<T>(parent);
        }

        public static string HiddenRuntimeInfo<T>(this HtmlHelper helper, TypeContext<T> parent)
        {
            if(typeof(EmbeddedEntity).IsAssignableFrom(typeof(T)))
                return helper.Hidden(helper.GlobalPrefixedName(TypeContext.Compose(parent.Name, EntityBaseKeys.RuntimeInfo)), new EmbeddedRuntimeInfo<T>(parent.Value, false).ToString());
            else
                return helper.Hidden(helper.GlobalPrefixedName(TypeContext.Compose(parent.Name, EntityBaseKeys.RuntimeInfo)), new RuntimeInfo<T>(parent.Value).ToString());
        }

        public static string HiddenStaticInfo<T>(this HtmlHelper helper, TypeContext<T> parent)
        {
            return helper.Hidden(helper.GlobalPrefixedName(TypeContext.Compose(parent.Name, EntityBaseKeys.StaticInfo)), new StaticInfo(parent.Type).ToString(), new { disabled = "disabled" });
        }

        public static string HiddenEntityInfo<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property)
        {
            TypeSubContext<S> typeContext = (TypeSubContext<S>)Common.WalkExpression(parent, property);
            return helper.HiddenRuntimeInfo<S>(typeContext) + helper.HiddenStaticInfo<S>(typeContext);
        }

        public static string HiddenRuntimeInfo<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property)
        {
            TypeSubContext<S> typeContext = (TypeSubContext<S>)Common.WalkExpression(parent, property);
            return helper.HiddenRuntimeInfo<S>(typeContext);
        }

        public static string HiddenStaticInfo<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property)
        {
            TypeSubContext<S> typeContext = (TypeSubContext<S>)Common.WalkExpression(parent, property);
            return helper.HiddenStaticInfo<S>(typeContext);
        }

        public static void WriteEntityInfo(this HtmlHelper helper, string prefix, RuntimeInfo runtimeInfo, StaticInfo staticInfo)
        {
            helper.Write(helper.HiddenEntityInfo(prefix, runtimeInfo, staticInfo));
        }

        public static void WriteRuntimeInfo(this HtmlHelper helper, string prefix, RuntimeInfo runtimeInfo)
        {
            helper.Write(helper.HiddenRuntimeInfo(prefix, runtimeInfo));
        }

        public static void WriteStaticInfo(this HtmlHelper helper, string prefix, StaticInfo staticInfo)
        {
            helper.Write(helper.HiddenStaticInfo(prefix, staticInfo));
        }

        public static void WriteEntityInfo<T>(this HtmlHelper helper, TypeContext<T> parent)
        {
            helper.Write(helper.HiddenEntityInfo<T>(parent));
        }

        public static void WriteRuntimeInfo<T>(this HtmlHelper helper, TypeContext<T> parent)
        {
            helper.Write(helper.HiddenRuntimeInfo<T>(parent));
        }

        public static void WriteStaticInfo<T>(this HtmlHelper helper, TypeContext<T> parent)
        {
            helper.Write(helper.HiddenStaticInfo<T>(parent));
        }

        public static void WriteEntityInfo<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property)
        {
            helper.HiddenEntityInfo<T, S>(parent, property);
        }

        public static void WriteRuntimeInfo<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property)
        {
            helper.Write(helper.HiddenRuntimeInfo<T, S>(parent, property));
        }

        public static void WriteStaticInfo<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property)
        {
            helper.Write(helper.HiddenStaticInfo<T, S>(parent, property));
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
                throw new ArgumentException("StaticInfo's StaticType must be set");

            Type cleanStaticType = Reflector.ExtractLite(StaticType) ?? StaticType;

            string staticTypeName = (Navigator.TypesToURLNames.ContainsKey(cleanStaticType)) ? cleanStaticType.Name : cleanStaticType.FullName;

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

        private string RuntimeTypeStr
        {
            get
            {
                if (RuntimeType == null)
                    return "";
                if (typeof(Lite).IsAssignableFrom(RuntimeType))
                    throw new ArgumentException("RuntimeInfo's RuntimeType cannot be of type Lite. Use ExtractLite or construct an RuntimeInfo<T> instead");
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
                IsNew ? "n" : "o",
                Ticks.TryToString()
                );
        }

        public static RuntimeInfo FromFormValue(string formValue)
        {
            if (string.IsNullOrEmpty(formValue))
                return null;

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

    public class RuntimeInfo<T> : RuntimeInfo
    {
        public RuntimeInfo(T Value)
        {
            if (typeof(EmbeddedEntity).IsAssignableFrom(Reflector.ExtractLite(typeof(T)) ?? typeof(T)))
                throw new ArgumentException("RuntimeInfo<T> cannot be called for an embedded entity. Call EmbeddedRuntimeInfo<T> instead");

            if (Value == null)
            {
                RuntimeType = null;
                return;
            }

            if (typeof(Lite).IsAssignableFrom(Value.GetType()))
            {
                Lite liteValue = Value as Lite;
                RuntimeType = liteValue.RuntimeType;
                IdOrNull = liteValue.IdOrNull;
                IsNew = liteValue.IdOrNull == null;
            }
            else
            {
                RuntimeType = Value.GetType();
                IIdentifiable identifiable = Value as IIdentifiable;
                if (identifiable == null)
                    throw new ArgumentException("Invalid type {0} for RuntimeInfo<T>. It must be Lite or Identifiable, otherwise call EmbeddedRuntimeInfo<T> or RuntimeInfo".Formato(RuntimeType));
                
                IdOrNull = identifiable.IdOrNull;
                IsNew = identifiable.IdOrNull == null;
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    public class EmbeddedRuntimeInfo<T> : RuntimeInfo
    {
        public EmbeddedRuntimeInfo(T value, bool isNew)
        {
            if (!typeof(EmbeddedEntity).IsAssignableFrom(typeof(T)))
                throw new ArgumentException("EmbeddedRuntimeInfo<T> cannot be called for a non embedded entity. Call RuntimeInfo<T> instead");

            this.IsNew = isNew;
            
            if (value == null)
                RuntimeType = null;
            else
                RuntimeType = value.GetType();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
