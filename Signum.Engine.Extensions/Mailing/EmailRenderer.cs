using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System.Web;
using System.Collections.Concurrent;
using System.Reflection;
using System.Resources;
using Signum.Entities.Mailing;
using System.IO;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.Mailing
{
    public static class EmailRenderer
    {
        static readonly Regex regex = new Regex(@"\{(\s*(?<token>\w(\:|\=)[_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}][_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}\p{Nd}]*)\s*\|?)+\}", RegexOptions.Singleline | RegexOptions.ExplicitCapture);
        static readonly Regex tokenRegex = new Regex(@"(?<prefix>\w)(?<separator>\:|\=)(?<literal>[_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}][_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}\p{Nd}]*)", RegexOptions.Singleline | RegexOptions.ExplicitCapture);

        public static string Replace(string content, IEmailModel model, object extendedData, LocalizedAssembly localizedAssembly)
        {
            List<Exception> exceptions = new List<Exception>();

            var result = regex.Replace(content, m =>
            {
                var captures = m.Groups["token"].Captures.Cast<Capture>().Select(c =>
                    {
                        try
                        {
                            return GetToken(c.Value, model, extendedData, localizedAssembly);
                        }
                        catch (Exception e)
                        {
                            exceptions.Add(e);
                            return null;
                        }
                    }).ToList();

                if (captures.Count == 1)
                    return captures[0];

                return captures[0].Formato(captures.Skip(1).ToArray());
            });

            if (exceptions.Count != 0)
                throw new AggregateException(exceptions);

            return result;
        }

        static string GetToken(string capture, IEmailModel model, object extendedData, LocalizedAssembly localizedAssembly)
        {
            Match m = tokenRegex.Match(capture);

            char prefix = m.Groups["prefix"].Value[0];

            string literal = m.Groups["literal"].Value;

            string text =
                prefix == 'X' ? GetValue(extendedData.ThrowIfNullC("extendedData is null"), literal) :
                prefix == 'M' ? GetValue(model, literal) :
                //prefix == 'T' ? GetValue(model.To, literal) :
                prefix == 'R' ? GetResource(localizedAssembly, literal) : null;

            char separator = m.Groups["separator"].Value[0];

            if (separator == '=')
                return text;

            return HttpUtility.HtmlEncode(text);
        }

        private static string GetResource(LocalizedAssembly localizedAssembly, string literal)
        {
            string typeName = literal.Before('.');

            LocalizedType lt = localizedAssembly.ThrowIfNullC("localizedAssembly is null").Types
                .Values.Where(a => a.Type.Name == typeName)
                .SingleOrDefaultEx(() => "Type {0} not found".Formato(typeName));

            return lt.Members.GetOrThrow(literal.After('.'), "{0} not found on " + lt.Type.Name);
        }

        static BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, Func<object, object>>> dictionary = new ConcurrentDictionary<Type, ConcurrentDictionary<string, Func<object, object>>>();

        static string GetValue(object value, string literal)
        {
            Type type = value.GetType();

            var f = dictionary.GetOrAdd(type, t => new ConcurrentDictionary<string, Func<object, object>>()).GetOrAdd(literal, p =>
            {
                MemberInfo mi = (MemberInfo)type.GetField(p, flags) ?? type.GetProperty(p, flags);

                if (mi == null)
                    throw new KeyNotFoundException("{0} not found on model of type {1}".Formato(literal, type.TypeName()));
                return ReflectionTools.CreateGetterUntyped(type, mi);
            });

            return f(value).TryToString();
        }
    }
}
