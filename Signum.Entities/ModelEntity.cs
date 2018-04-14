using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Signum.Utilities;

namespace Signum.Entities
{
    [Serializable]
    public abstract class ModelEntity : ModifiableEntity, IRootEntity
    {
        protected internal override void PreSaving(PreSavingContext ctx)
        {
        
        }

        protected internal override void PostRetrieving()
        {
            throw new InvalidOperationException("ModelEntities are not meant to be retrieved"); 
        }

        public override string ToString()
        {
            return "";
        }

        public static Implementations GetImplementations(PropertyRoute route)
        {
            if (!typeof(ModelEntity).IsAssignableFrom(route.RootType))
                throw new InvalidOperationException("Route {0} is not rooted on a {1}".FormatWith(route, typeof(ModifiableEntity).Name));

            PropertyRoute fieldRoute = route;
            if (fieldRoute.PropertyRouteType == PropertyRouteType.LiteEntity)
                fieldRoute = fieldRoute.Parent;

            if (fieldRoute.PropertyRouteType == PropertyRouteType.MListItems)
                fieldRoute = fieldRoute.Parent;
            
            return Implementations.FromAttributes(
                route.Type.CleanType(),
                route,
                fieldRoute.FieldInfo.GetCustomAttribute<ImplementedByAttribute>(),
                fieldRoute.FieldInfo.GetCustomAttribute<ImplementedByAllAttribute>());
        }
    }
}
