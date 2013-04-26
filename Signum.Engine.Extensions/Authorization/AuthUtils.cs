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
            baseRules => Merge(baseRules, MaxTypeAllowed, TypeAllowed.Create, TypeAllowed.None));

        public static readonly DefaultBehaviour<TypeAllowedAndConditions> MinType = new DefaultBehaviour<TypeAllowedAndConditions>(
              new TypeAllowedAndConditions(TypeAllowed.None),
           baseRules => Merge(baseRules, MinTypeAllowed, TypeAllowed.None, TypeAllowed.Create));

        static TypeAllowedAndConditions Merge(IEnumerable<TypeAllowedAndConditions> baseRules, Func<IEnumerable<TypeAllowed>, TypeAllowed> maxMerge, TypeAllowed max, TypeAllowed min)
        {
            TypeAllowedAndConditions only = baseRules.Only();
            if (only != null)
                return only;

            if (baseRules.Any(a => a.Exactly(max)))
                return new TypeAllowedAndConditions(max);

            TypeAllowedAndConditions onlyNotOposite = baseRules.Where(a => !a.Exactly(min)).Only();
            if (onlyNotOposite != null)
                return onlyNotOposite;

            var first = baseRules.FirstOrDefault(c => !c.Conditions.IsNullOrEmpty());

            if (first == null)
                return new TypeAllowedAndConditions(maxMerge(baseRules.Select(a => a.Fallback)));

            var conditions = first.Conditions.Select(c => c.ConditionName).ToList();

            if (baseRules.Where(c => !c.Conditions.IsNullOrEmpty() && c != first).Any(br => !br.Conditions.Select(c => c.ConditionName).SequenceEqual(conditions)))
                return new TypeAllowedAndConditions(TypeAllowed.None);

            return new TypeAllowedAndConditions(maxMerge(baseRules.Select(a => a.Fallback)),
                conditions.Select((c, i) => new TypeConditionRule(c, maxMerge(baseRules.Where(br => !br.Conditions.IsNullOrEmpty()).Select(br => br.Conditions[i].Allowed)))).ToArray());
        }   

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
