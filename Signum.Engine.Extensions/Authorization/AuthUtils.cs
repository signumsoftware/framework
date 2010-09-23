using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Entities.Authorization;

namespace Signum.Engine.Authorization
{
    static class AuthUtils
    {
        public static readonly DefaultBehaviour<bool> MaxBool = new DefaultBehaviour<bool>(true, col => col.Any(a => a));
        public static readonly DefaultBehaviour<bool> MinBool = new DefaultBehaviour<bool>(false, col => col.All(a => a));

        public static readonly DefaultBehaviour<PropertyAllowed> MaxProperty = new DefaultBehaviour<PropertyAllowed>(PropertyAllowed.Modify, MaxPropertyAllowed);
        public static readonly DefaultBehaviour<PropertyAllowed> MinProperty = new DefaultBehaviour<PropertyAllowed>(PropertyAllowed.None, MinPropertyAllowed);

        public static readonly DefaultBehaviour<TypeAllowed> MaxType = new DefaultBehaviour<TypeAllowed>(TypeAllowed.DBCreateUICreate, MaxTypeAllowed);
        public static readonly DefaultBehaviour<TypeAllowed> MinType = new DefaultBehaviour<TypeAllowed>(TypeAllowed.DBNoneUINone, MinTypeAllowed);

        public static readonly DefaultBehaviour<EntityGroupAllowedDN> MaxEntityGroup = new DefaultBehaviour<EntityGroupAllowedDN>(EntityGroupAllowedDN.CreateCreate,
            col => new EntityGroupAllowedDN(
                MaxTypeAllowed(col.Select(a => a.InGroup)),
                MaxTypeAllowed(col.Select(a => a.OutGroup))));
        public static readonly DefaultBehaviour<EntityGroupAllowedDN> MinEntityGroup = new DefaultBehaviour<EntityGroupAllowedDN>(EntityGroupAllowedDN.NoneNone,
               col => new EntityGroupAllowedDN(
                MinTypeAllowed(col.Select(a => a.InGroup)),
                MinTypeAllowed(col.Select(a => a.OutGroup))));

        static TypeAllowed MaxTypeAllowed(this IEnumerable<TypeAllowed> collection)
        {
            TypeAllowed result = TypeAllowed.DBNoneUINone;

            foreach (var item in collection)
            {
                if (item > result)
                    result = item;

                if (result == TypeAllowed.DBCreateUICreate)
                    return result;
                
            }
            return result;
        }

        static TypeAllowed MinTypeAllowed(this IEnumerable<TypeAllowed> collection)
        {
            TypeAllowed result = TypeAllowed.DBCreateUICreate;

            foreach (var item in collection)
            {
                if (item < result)
                    result = item;

                if (result == TypeAllowed.DBNoneUINone)
                    return result;

            }
            return result;
        }

        static PropertyAllowed MaxPropertyAllowed(this IEnumerable<PropertyAllowed> collection)
        {
            PropertyAllowed result = PropertyAllowed.None;

            foreach (var item in collection)
            {
                if (item > result)
                    result = item;

                if (result == PropertyAllowed.Modify)
                    return result;

            }
            return result;
        }

        static PropertyAllowed MinPropertyAllowed(this IEnumerable<PropertyAllowed> collection)
        {
            PropertyAllowed result = PropertyAllowed.Modify;

            foreach (var item in collection)
            {
                if (item < result)
                    result = item;

                if (result == PropertyAllowed.None)
                    return result;

            }
            return result;
        }

    }
}
