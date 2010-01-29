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
        public static string HiddenSFInfo(this HtmlHelper helper, string prefix, EntityInfo entityInfo)
        {
            return helper.Hidden(TypeContext.Compose(prefix, EntityBaseKeys.Info), entityInfo.ToString());
        }

        public static string HiddenSFInfo<T>(this HtmlHelper helper, TypeContext<T> parent)
        {
            if(typeof(EmbeddedEntity).IsAssignableFrom(typeof(T)))
                return helper.Hidden(helper.GlobalPrefixedName(TypeContext.Compose(parent.Name, EntityBaseKeys.Info)), new EmbeddedEntityInfo<T>(parent.Value, false).ToString());
            else
                return helper.Hidden(helper.GlobalPrefixedName(TypeContext.Compose(parent.Name, EntityBaseKeys.Info)), new EntityInfo<T>(parent.Value).ToString());
        }

        public static string HiddenSFInfo<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property)
        {
            TypeSubContext<S> typeContext = (TypeSubContext<S>)Common.WalkExpression(parent, property);
            if(typeof(EmbeddedEntity).IsAssignableFrom(typeof(S)))
                return helper.Hidden(helper.GlobalPrefixedName(TypeContext.Compose(typeContext.Name, EntityBaseKeys.Info)), new EmbeddedEntityInfo<S>(typeContext.Value, false).ToString());
            else
                return helper.Hidden(helper.GlobalPrefixedName(TypeContext.Compose(typeContext.Name, EntityBaseKeys.Info)), new EntityInfo<S>(typeContext.Value).ToString());
        }

        public static void WriteSFInfo(this HtmlHelper helper, string prefix, EntityInfo entityInfo)
        {
            helper.Write(helper.HiddenSFInfo(prefix, entityInfo));
        }

        public static void WriteSFInfo<T>(this HtmlHelper helper, TypeContext<T> parent)
        {
            helper.Write(helper.HiddenSFInfo<T>(parent));
        }

        public static void WriteSFInfo<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property)
        {
            helper.Write(helper.HiddenSFInfo<T, S>(parent, property));
        }
    }

    public class EntityInfo
    {
        public Type StaticType { get; set; }
        public Type RuntimeType { get; set; }
        public int? IdOrNull { get; set; }
        public bool IsEmbedded 
        {
            get { return typeof(EmbeddedEntity).IsAssignableFrom(StaticType); }
        }
        public bool IsNew { get; set; }
        public long? Ticks { get; set; }

        private string RuntimeTypeStr
        {
            get
            {
                if (RuntimeType == null)
                    return "";
                if (typeof(Lite).IsAssignableFrom(RuntimeType))
                    throw new ArgumentException("EntityInfo's RuntimeType cannot be of type Lite. Use ExtractLite or construct an EntityInfo<T> instead");
                return RuntimeType.Name;
            }
        }

        public override string ToString()
        {
            if (StaticType == null)
                throw new ArgumentException("EntityInfo's StaticType must be set");

            if (IdOrNull != null && IsNew)
                throw new ArgumentException("Invalid EntityInfo parameters: IdOrNull={0} and IsNew=true".Formato(IdOrNull));

            Type cleanStaticType = Reflector.ExtractLite(StaticType) ?? StaticType;

            string staticTypeName = (Navigator.TypesToURLNames.ContainsKey(cleanStaticType)) ? cleanStaticType.Name : cleanStaticType.FullName;

            return "{0};{1};{2};{3};{4};{5}".Formato(
                staticTypeName,
                RuntimeTypeStr,
                IdOrNull.TryToString(),
                IsEmbedded ? "e" : "i",
                IsNew ? "n" : "o",
                Ticks.TryToString()
                );
        }

        public static EntityInfo FromFormValue(string formValue)
        {
            if (string.IsNullOrEmpty(formValue))
                return null;

            string[] parts = formValue.Split(new[] { ";" }, StringSplitOptions.None);
            if (parts.Length != 6)
                throw new ArgumentException("Incorrect sfInfo format: {0}".Formato(formValue));

            Type runtimeType = parts[1].HasText() ? Navigator.ResolveType(parts[1]) : null;

            Type staticType = runtimeType; //If there's a runtime type, I don't need the staticType
            if (runtimeType == null)
            {
                try
                { 
                    staticType = Navigator.ResolveType(parts[0]);               
                }catch{}
            }
            return new EntityInfo
            {
                StaticType = staticType, 
                RuntimeType = runtimeType,
                IdOrNull = (parts[2].HasText()) ? int.Parse(parts[2]) : (int?)null,
                IsNew = parts[4]=="n" ? true : false,
                Ticks = (parts[5].HasText()) ? long.Parse(parts[5]) : (long?)null
            };
        }
    }

    public class EntityInfo<T> : EntityInfo
    {
        public EntityInfo(T Value)
        {
            this.StaticType = Reflector.ExtractLite(typeof(T)) ?? typeof(T);

            if (typeof(EmbeddedEntity).IsAssignableFrom(this.StaticType))
                throw new ArgumentException("EntityInfo<T> cannot be called for an embedded entity. Call EmbeddedEntityInfo<T> instead");

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
                    throw new ArgumentException("Invalid type {0} for EntityInfo<T>. It must be Lite or Identifiable, otherwise call EmbeddedEntityInfo<T> or EntityInfo".Formato(RuntimeType));
                
                IdOrNull = identifiable.IdOrNull;
                IsNew = identifiable.IdOrNull == null;
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    public class EmbeddedEntityInfo<T> : EntityInfo
    {
        public EmbeddedEntityInfo(T value, bool isNew)
        {
            if (!typeof(EmbeddedEntity).IsAssignableFrom(typeof(T)))
                throw new ArgumentException("EmbeddedEntity<T> cannot be called for a non embedded entity. Call EntityInfo<T> instead");

            this.StaticType = typeof(T);
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
