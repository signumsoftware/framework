using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;
using System.Data;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using Signum.Engine.Linq;
using System.Collections;
using Signum.Engine.Maps;
using Signum.Entities.Basics;
using System.Globalization;

namespace Signum.Engine.DynamicQuery
{
    public static class AutocompleteUtils
    {
        public static List<Lite<IdentifiableEntity>> FindLiteLike(Implementations implementations, string subString, int count)
        {
            if (implementations.IsByAll)
                throw new InvalidOperationException("ImplementedByAll not supported for FindLiteLike");

            try
            {
                using (ExecutionMode.UserInterface())
                    return FindLiteLike(implementations.Types, subString, count);
            }
            catch (Exception e)
            {
                e.Data["implementations"] = implementations.ToString();
                throw;
            }
        }


        static NumberStyles numberStyles = NumberStyles.Integer | NumberStyles.AllowThousands;

        static List<Lite<IdentifiableEntity>> FindLiteLike(IEnumerable<Type> types, string subString, int count)
        {
            types = types.Where(t => Schema.Current.IsAllowed(t) == null);

            List<Lite<IdentifiableEntity>> results = new List<Lite<IdentifiableEntity>>();
            int? id = subString.ToInt(numberStyles);
            if (id.HasValue)
            {
                foreach (var t in types)
                {
                    var lite = miLiteById.GetInvoker(t).Invoke(id.Value);
                    if (lite != null)
                    {
                        results.Add(lite);

                        if (results.Count >= count)
                            return results;
                    }
                }
            }
            else
            {
                if (subString.Trim('\'', '"').ToInt(numberStyles).HasValue)
                    subString = subString.Trim('\'', '"');

                var parts = subString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var t in types)
                {
                    results.AddRange(miLiteContaining.GetInvoker(t)(parts, count - results.Count));

                    if (results.Count >= count)
                        return results;
                }
            }

            return results;
        }

        public static List<Lite<T>> Autocomplete<T>(this IQueryable<Lite<T>> query, string subString, int count)
            where T : IdentifiableEntity
        {
            using(ExecutionMode.UserInterface())
            {

                List<Lite<T>> results = new List<Lite<T>>();

                int? id = subString.ToInt();
                if (id.HasValue)
                {
                    Lite<T> entity = query.SingleOrDefaultEx(e => e.Id == id);

                    if (entity != null)
                        results.Add(entity);

                    if (results.Count >= count)
                        return results;
                }

                if (subString.Trim('\'', '"').ToInt(numberStyles).HasValue)
                    subString = subString.Trim('\'', '"');

                var parts = subString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                results.AddRange(query.Where(a => a.ToString().ContainsAll(parts))
                    .OrderBy(a => a.ToString().Length)
                    .Take(count - results.Count));

                return results;
            }
        }

        public static List<Lite<T>> Autocomplete<T>(this IEnumerable<Lite<T>> query, string subString, int count)
            where T : IdentifiableEntity
        {
            using (ExecutionMode.UserInterface())
            {
                List<Lite<T>> results = new List<Lite<T>>();

                int? id = subString.ToInt();
                if (id.HasValue)
                {
                    Lite<T> entity = query.SingleOrDefaultEx(e => e.Id == id);

                    if (entity != null)
                        results.Add(entity);

                    if (results.Count >= count)
                        return results;
                }

                if (subString.Trim('\'', '"').ToInt(numberStyles).HasValue)
                    subString = subString.Trim('\'', '"');

                var parts = subString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                results.AddRange(query.Where(a =>  a.ToString().ContainsAll(parts))
                    .OrderBy(a => a.ToString().Length)
                    .Take(count - results.Count));

                return results;
            }
        }

        static GenericInvoker<Func<int, Lite<IdentifiableEntity>>> miLiteById =
            new GenericInvoker<Func<int, Lite<IdentifiableEntity>>>(id => LiteById<TypeDN>(id));
        static Lite<IdentifiableEntity> LiteById<T>(int id)
            where T : IdentifiableEntity
        {
            return Database.Query<T>().Where(a => a.id == id).Select(a => a.ToLite()).SingleOrDefault();
        }

        static GenericInvoker<Func<string[], int, List<Lite<IdentifiableEntity>>>> miLiteContaining =
            new GenericInvoker<Func<string[], int, List<Lite<IdentifiableEntity>>>>((parts, c) => LiteContaining<TypeDN>(parts, c));
        static List<Lite<IdentifiableEntity>> LiteContaining<T>(string[] parts, int count)
            where T : IdentifiableEntity
        {
            return Database.Query<T>()
                .Where(a => a.ToString().ContainsAll(parts))
                .OrderBy(a => a.ToString().Length)
                .Select(a => a.ToLite())
                .Take(count)
                .AsEnumerable()
                .Cast<Lite<IdentifiableEntity>>()
                .ToList();
        }

        public static List<Lite<IdentifiableEntity>> FindAllLite(Implementations implementations)
        {
            if (implementations.IsByAll)
                throw new InvalidOperationException("ImplementedByAll is not supported for RetrieveAllLite");

            try
            {
                using (ExecutionMode.UserInterface())
                    return implementations.Types.SelectMany(type => Database.RetrieveAllLite(type)).ToList();
            }
            catch (Exception e)
            {
                e.Data["implementations"] = implementations.ToString();
                throw;
            }
        }
    }
}
