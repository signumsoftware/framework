using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;

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
                if (obj is T t)
                    yield return t;
                else if (obj is string s && typeof(T).IsEnum && Enum.IsDefined(typeof(T), s))
                    yield return (T)Enum.Parse(typeof(T), s);
                else if(obj is IComparable && ReflectionTools.IsNumber(obj.GetType()) && ReflectionTools.IsNumber(typeof(T)))
                {
                    if (ReflectionTools.IsDecimalNumber(obj.GetType()) &&
                        !ReflectionTools.IsDecimalNumber(typeof(T)))
                        throw new InvalidOperationException($"Converting {obj} ({obj.GetType().TypeName()}) to {typeof(T).GetType().TypeName()} would lose precission");

                    yield return ReflectionTools.ChangeType<T>(obj);
                }
                else if (obj is List<object> list)
                    yield return (T)giConvertListTo.GetInvoker(typeof(T).ElementType())(list);
            }
        }

        static readonly GenericInvoker<Func<List<object>,IList>> giConvertListTo = new GenericInvoker<Func<List<object>, IList>>(list => ConvertListTo<int>(list));

        static List<S> ConvertListTo<S>(List<object> list)
        {
            return list.Cast<S>().ToList();
        }
    }
}
