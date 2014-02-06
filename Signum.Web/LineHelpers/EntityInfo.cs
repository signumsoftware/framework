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
            return helper.Hidden(tc.Compose(EntityBaseKeys.RuntimeInfo), tc.RuntimeInfo().TryToString());
        }

        public static MvcHtmlString HiddenStaticInfo(this HtmlHelper helper, EntityBase tc)
        {
            Type type = tc is EntityListBase ? ((EntityListBase)tc).ElementType : tc.Type;

            PropertyRoute pr =
                !type.IsEmbeddedEntity() ? null :
                tc is EntityListBase ? tc.PropertyRoute.Add("Item") :
                tc.PropertyRoute;

            StaticInfo si = new StaticInfo(type, tc.Implementations, pr, tc.ReadOnly);
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
       

        public StaticInfo(Type staticType, Implementations? implementations , PropertyRoute embeddedRoute, bool readOnly)
        {
            if (staticType.IsEmbeddedEntity())
            {
                if (implementations != null)
                    throw new ArgumentException("implementations should be null for EmbeddedEntities");

                Types = new[] { staticType };

                if (embeddedRoute == null)
                    throw new ArgumentNullException("embeddedRoute"); 

                EmbeddedRoute = embeddedRoute;
            }
            else
            {
                Types = implementations.Value.IsByAll ? ImplementedByAll :
                        implementations.Value.Types.ToArray();
            }

            this.IsReadOnly = readOnly;
        }

        public Type[] Types { get; private set; }
        public PropertyRoute EmbeddedRoute { get; private set;}
 
        public bool IsEmbedded
        {
            get { return Types != null && Types.Length == 1 && typeof(EmbeddedEntity).IsAssignableFrom(Types[0]); }
        }

        public bool IsReadOnly { get; private set; }

        public override string ToString()
        {
            if (Types == null)
                throw new ArgumentException("StaticInfo.Types must be set");

            return "{0};{1};{2};{3};{4};{5}".Formato(
                    Types == ImplementedByAll ? ImplementedByAllKey : Types.ToString(t => Navigator.ResolveWebTypeName(t), ","),
                    Types == ImplementedByAll ? ImplementedByAllKey : Types.ToString(t => t.NiceName(), ","),
                    IsEmbedded ? "e" : "i",
                    IsReadOnly ? "r" : "",
                    EmbeddedRoute != null ? Navigator.ResolveWebTypeName(EmbeddedRoute.RootType) : "",
                    EmbeddedRoute != null ? EmbeddedRoute.PropertyString() : ""
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
        public Type EntityType { get; private set; }
        public int? IdOrNull { get; private set; }
        public bool IsNew { get; private set; }
        public long? Ticks { get; private set; }

        public RuntimeInfo(Type type, int? idOrNull, bool isNew, long? ticks) 
        {
            if(type == null)
                throw new ArgumentNullException("type"); 

            this.EntityType = type;
            this.IdOrNull = idOrNull;
            this.IsNew = isNew;
            this.Ticks = ticks;
        }

        public RuntimeInfo(Lite<IIdentifiable> lite) :
            this(lite.EntityType, lite.IdOrNull, lite.IdOrNull == null, null)
        {
        }

        public RuntimeInfo(EmbeddedEntity entity): 
            this(entity.GetType(), null, false, null)
        {
        }

        public RuntimeInfo(IIdentifiable entity)
            : this(entity.GetType(), entity.IdOrNull, entity.IsNew,
                 entity is Entity ? ((Entity)entity).Ticks : (long?)null)
        {
        }

        public override string ToString()
        {
            if (IdOrNull != null && IsNew)
                throw new ArgumentException("Invalid RuntimeInfo parameters: IdOrNull={0} and IsNew=true".Formato(IdOrNull));

            if (EntityType != null && EntityType.IsLite())
                throw new ArgumentException("RuntimeInfo's RuntimeType cannot be of type Lite. Use ExtractLite or construct a RuntimeInfo<T> instead");

            return "{0};{1};{2};{3}".Formato(
                Navigator.ResolveWebTypeName(EntityType),
                IdOrNull.TryToString(),
                IsNew ? "n" : "o",
                Ticks
                );
        }

        public static RuntimeInfo FromFormValue(string formValue)
        {
            if(string.IsNullOrEmpty(formValue))
                return null;

            string[] parts = formValue.Split(new[] { ";" }, StringSplitOptions.None);
            if (parts.Length != 4)
                throw new ArgumentException("Incorrect sfRuntimeInfo format: {0}".Formato(formValue));

            string entityTypeString = parts[0];

            return new RuntimeInfo(
                Navigator.ResolveType(entityTypeString),
                (parts[1].HasText()) ? int.Parse(parts[1]) : (int?)null,
                parts[2] == "n",
                parts.Length == 4 && parts[3].HasText() ? long.Parse(parts[3]) : (long?)null
            );
        }

        public Lite<IdentifiableEntity> ToLite()
        {
            if (IsNew)
                throw new InvalidOperationException("The RuntimeInfo represents a new entity");

            return Lite.Create(this.EntityType, this.IdOrNull.Value);
        }
    }
}
