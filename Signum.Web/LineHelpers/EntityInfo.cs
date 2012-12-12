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
        public static MvcHtmlString HiddenLite(this HtmlHelper helper, string name, Lite<IIdentifiable> lite)
        {
            return helper.Hidden(name, lite.Key());
        }

        public static MvcHtmlString HiddenEntityInfo(this HtmlHelper helper, EntityBase tc)
        {
            return helper.HiddenRuntimeInfo(tc).Concat(helper.HiddenStaticInfo(tc));
        }

        public static MvcHtmlString HiddenRuntimeInfo(this HtmlHelper helper, TypeContext tc)
        {
            return helper.Hidden(tc.Compose(EntityBaseKeys.RuntimeInfo), tc.RuntimeInfo().ToString());
        }

        public static MvcHtmlString HiddenStaticInfo(this HtmlHelper helper, EntityBase tc)
        {
            Type type = tc is EntityListBase ? ((EntityListBase)tc).ElementType : tc.Type;
            StaticInfo si = new StaticInfo(type, tc.Implementations) { IsReadOnly = tc.ReadOnly };
            return helper.Hidden(tc.Compose(EntityBaseKeys.StaticInfo), si.ToString(), new { disabled = "disabled" });
        }

        public static MvcHtmlString HiddenRuntimeInfo<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property)
        {
            TypeContext<S> typeContext = Common.WalkExpression(parent, property);
            return helper.HiddenRuntimeInfo(typeContext);
        }
    }

    public class StaticInfo
    {
        public static readonly Type[] ImplementedByAll = new Type[0];
        public static readonly string ImplementedByAllKey = "[All]";

        public StaticInfo(Type staticType, Implementations? implementations)
        {
            if (staticType.IsEmbeddedEntity())
            {
                if (implementations != null)
                    throw new ArgumentException("implementations should be null for EmbeddedEntities");

                Types = new[] { staticType };
            }
            else
            {
                Types = implementations.Value.IsByAll ? ImplementedByAll :
                        implementations.Value.Types.ToArray();
            }
        }

        public Type[] Types { get; set; }

        public bool IsEmbedded
        {
            get { return Types != null && Types.Length == 1 && typeof(EmbeddedEntity).IsAssignableFrom(Types[0]); }
        }

        public bool IsReadOnly { get; set; }

        public override string ToString()
        {
            if (Types == null)
                throw new ArgumentException("StaticInfo.Types must be set");

            return "{0};{1};{2}".Formato(
                    Types == ImplementedByAll ? ImplementedByAllKey : 
                    Types.ToString(t => Navigator.ResolveWebTypeName(t), ","),
                    IsEmbedded ? "e" : "i",
                    IsReadOnly ? "r" : ""
                );
        }

        public static Type[] ParseTypes(string types)
        {
            if (string.IsNullOrEmpty(types))
                throw new ArgumentNullException("types");

            if (types == ImplementedByAllKey)
                return ImplementedByAll;

            return types.Split(',').Select(tn => Navigator.ResolveType(tn)).NotNull().ToArray();
        }
    }

    public class RuntimeInfo
    {
        public Type RuntimeType { get; set; }
        public int? IdOrNull { get; set; }
        public bool IsNew { get; set; }

        public RuntimeInfo() { }

        public RuntimeInfo(Lite<IIdentifiable> lite)
        {
            if (lite == null)
            {
                RuntimeType = null;
                return;
            }

            RuntimeType = lite.RuntimeType;
            IdOrNull = lite.IdOrNull;
            IsNew = lite.IdOrNull == null;
        }

        public RuntimeInfo(EmbeddedEntity entity)
        {
            if (entity == null)
            {
                RuntimeType = null;
                return;
            }

            RuntimeType = entity.GetType();
        }

        public RuntimeInfo(IIdentifiable entity)
        {
            if (entity == null)
            {
                RuntimeType = null;
                return;
            }

            RuntimeType = entity.GetType();
            IdOrNull = entity.IdOrNull;
            IsNew = entity.IsNew;
        }

        public override string ToString()
        {
            if (IdOrNull != null && IsNew)
                throw new ArgumentException("Invalid RuntimeInfo parameters: IdOrNull={0} and IsNew=true".Formato(IdOrNull));

            if (RuntimeType != null && RuntimeType.IsLite())
                throw new ArgumentException("RuntimeInfo's RuntimeType cannot be of type Lite. Use ExtractLite or construct a RuntimeInfo<T> instead");

            return "{0};{1};{2}".Formato(
                (RuntimeType == null) ? "" : Navigator.ResolveWebTypeName(RuntimeType),
                IdOrNull.TryToString(),
                IsNew ? "n" : "o"
                );
        }

        public static RuntimeInfo FromFormValue(string formValue)
        {
            string[] parts = formValue.Split(new[] { ";" }, StringSplitOptions.None);
            if (parts.Length != 3)
                throw new ArgumentException("Incorrect sfRuntimeInfo format: {0}".Formato(formValue));

            string runtimeTypeString = parts[0];

            return new RuntimeInfo
            {
                RuntimeType = string.IsNullOrEmpty(runtimeTypeString) ? null : Navigator.ResolveType(runtimeTypeString),
                IdOrNull = (parts[1].HasText()) ? int.Parse(parts[1]) : (int?)null,
                IsNew = parts[2]=="n" ? true : false
            };
        }

        public Lite<IdentifiableEntity> ToLite()
        {
            if(IsNew)
                throw new InvalidOperationException("The RuntimeInfo represents a new entity");

            return Lite.Create(this.RuntimeType, this.IdOrNull.Value);
        }
    }
}
