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
            return args.SmartConvertTo<T>().SingleEx(() => "{0} in the argument list".FormatWith(typeof(T))); ;
        }

        public static T TryGetArgC<T>(this IEnumerable<object> args) where T : class
        {
            return args.SmartConvertTo<T>().SingleOrDefaultEx(
                () => "There are more than one {0} in the argument list".FormatWith(typeof(T)));
        }

        public static T? TryGetArgS<T>(this IEnumerable<object> args) where T : struct
        {
            var casted = args.SmartConvertTo<T>();

            if (casted.IsEmpty())
                return null;

            return casted.SingleEx(() => "{0} in the argument list".FormatWith(typeof(T)));
        }

        static IEnumerable<T> SmartConvertTo<T>(this IEnumerable<object> args)
        {
            if (args == null)
                yield break;

            foreach (var obj in args)
            {
                switch (obj)
                {
                    case T t:
                        yield return t;
                        break;

                    case string s when typeof(T).IsEnum && Enum.IsDefined(typeof(T), s):
                        yield return (T)Enum.Parse(typeof(T), s);
                        break;

                    case List<object> list:
                        yield return (T)giConvertListTo.GetInvoker(typeof(T).ElementType())(list);
                        break;

                    default:
                        //Skip
                        break;
                }
            }
        }

        static readonly GenericInvoker<Func<List<object>,IList>> giConvertListTo = new GenericInvoker<Func<List<object>, IList>>(list => ConvertListTo<int>(list));

        static List<S> ConvertListTo<S>(List<object> list)
        {
            return list.Cast<S>().ToList();
        }
    }
}
