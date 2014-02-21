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

        public static MvcHtmlString HiddenRuntimeInfo(this HtmlHelper helper, TypeContext tc)
        {
            return helper.Hidden(tc.Compose(EntityBaseKeys.RuntimeInfo), tc.RuntimeInfo().TryToString());
        }

        public static MvcHtmlString HiddenRuntimeInfo<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property)
        {
            TypeContext<S> typeContext = Common.WalkExpression(parent, property);
            return helper.HiddenRuntimeInfo(typeContext);
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
