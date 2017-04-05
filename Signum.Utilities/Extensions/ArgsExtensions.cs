using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Reflection;

namespace Signum.Utilities
{
    public static class ArgsExtensions
    {
        public static T GetArg<T>(this IEnumerable<object> args)
        {
            return args.OfTypeOrEmpty<T>().SingleEx(() => "{0} in the argument list".FormatWith(typeof(T))); ;
        }

        public static T TryGetArgC<T>(this IEnumerable<object> args) where T : class
        {
            return args.OfTypeOrEmpty<T>().SingleOrDefaultEx(
                () => "There are more than one {0} in the argument list".FormatWith(typeof(T)));
        }

        public static T? TryGetArgS<T>(this IEnumerable<object> args) where T : struct
        {
            var casted = args.OfTypeOrEmpty<T>();

            if (casted.IsEmpty())
                return null;

            return casted.SingleEx(() => "{0} in the argument list".FormatWith(typeof(T)));
        }

        static IEnumerable<T> OfTypeOrEmpty<T>(this IEnumerable<object> args)
        {
            if (args == null)
                return Enumerable.Empty<T>();

            return args
                .Where(o => o is T || (o is string && typeof(T).IsEnum) || (o is List<object> && typeof(T).IsInstantiationOf(typeof(List<>))))
                .Select(o => o is T ? (T)o :
                                o is string ? (T)Enum.Parse(typeof(T), (string)o) :
                                    (T)giConvertListTo.GetInvoker(typeof(T).ElementType())((List<object>)o));
        }

        static readonly GenericInvoker<Func<List<object>,IList>> giConvertListTo = new GenericInvoker<Func<List<object>, IList>>(list => ConvertListTo<int>(list));

        static List<S> ConvertListTo<S>(List<object> list)
        {
            return list.Cast<S>().ToList();
        }
    }
}
