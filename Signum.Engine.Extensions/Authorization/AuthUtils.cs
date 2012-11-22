using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Entities.Authorization;
using Signum.Entities;
using System.Collections.ObjectModel;

namespace Signum.Engine.Authorization
{
    static class AuthUtils
    {
        public static readonly DefaultBehaviour<bool> MaxBool = new DefaultBehaviour<bool>(true, col => col.Any(a => a));
        public static readonly DefaultBehaviour<bool> MinBool = new DefaultBehaviour<bool>(false, col => col.All(a => a));

        public static readonly DefaultBehaviour<OperationAllowed> MaxOperation = new DefaultBehaviour<OperationAllowed>(OperationAllowed.Allow, MaxPropertyAllowed);
        public static readonly DefaultBehaviour<OperationAllowed> MinOperation = new DefaultBehaviour<OperationAllowed>(OperationAllowed.None, MinPropertyAllowed);
        
        public static readonly DefaultBehaviour<PropertyAllowed> MaxProperty = new DefaultBehaviour<PropertyAllowed>(PropertyAllowed.Modify, MaxPropertyAllowed);
        public static readonly DefaultBehaviour<PropertyAllowed> MinProperty = new DefaultBehaviour<PropertyAllowed>(PropertyAllowed.None, MinPropertyAllowed);

        static ReadOnlyCollection<TypeConditionRule> None = Enumerable.Empty<TypeConditionRule>().ToReadOnly();

        public static readonly DefaultBehaviour<TypeAllowedAndConditions> MaxType = new DefaultBehaviour<TypeAllowedAndConditions>(
            new TypeAllowedAndConditions(TypeAllowed.Create),
            baseRules => new TypeAllowedAndConditions(baseRules.Select(a=>a.Fallback).MaxTypeAllowed(),
                baseRules.Only().TryCC(a => a.Conditions) ?? None));

        public static readonly DefaultBehaviour<TypeAllowedAndConditions> MinType = new DefaultBehaviour<TypeAllowedAndConditions>(
              new TypeAllowedAndConditions(TypeAllowed.None),
            baseRules => new TypeAllowedAndConditions(baseRules.Select(a=>a.Fallback).MinTypeAllowed(),
                baseRules.Only().TryCC(a => a.Conditions) ?? None));
            

        static TypeAllowed MaxTypeAllowed(this IEnumerable<TypeAllowed> collection)
        {
            TypeAllowed result = TypeAllowed.None;

            foreach (var item in collection)
            {
                if (item > result)
                    result = item;

                if (result == TypeAllowed.Create)
                    return result;
                
            }
            return result;
        }

        static TypeAllowed MinTypeAllowed(this IEnumerable<TypeAllowed> collection)
        {
            TypeAllowed result = TypeAllowed.Create;

            foreach (var item in collection)
            {
                if (item < result)
                    result = item;

                if (result == TypeAllowed.None)
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


        static OperationAllowed MaxPropertyAllowed(this IEnumerable<OperationAllowed> collection)
        {
            OperationAllowed result = OperationAllowed.None;

            foreach (var item in collection)
            {
                if (item > result)
                    result = item;

                if (result == OperationAllowed.Allow)
                    return result;

            }
            return result;
        }

        static OperationAllowed MinPropertyAllowed(this IEnumerable<OperationAllowed> collection)
        {
            OperationAllowed result = OperationAllowed.Allow;

            foreach (var item in collection)
            {
                if (item < result)
                    result = item;

                if (result == OperationAllowed.None)
                    return result;

            }
            return result;
        }

    }
}
