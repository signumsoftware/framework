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
        public static List<Lite<Entity>> FindLiteLike(Implementations implementations, string subString, int count)
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

        static List<Lite<Entity>> FindLiteLike(IEnumerable<Type> types, string subString, int count)
        {
            types = types.Where(t => Schema.Current.IsAllowed(t, inUserInterface: true) == null);

            List<Lite<Entity>> results = new List<Lite<Entity>>();
          
            foreach (var t in types)
            {
                PrimaryKey id;
                if (PrimaryKey.TryParse(subString, t, out id))
                {
                    var lite = miLiteById.GetInvoker(t).Invoke(id);
                    if (lite != null)
                    {
                        results.Add(lite);

                        if (results.Count >= count)
                            return results;
                    }
                }
            }


            foreach (var t in types)
            {
                PrimaryKey id;
                if (!PrimaryKey.TryParse(subString, t, out id))
                {
                    var parts = subString.Trim('\'', '"').SplitNoEmpty(' ');

                    results.AddRange(miLiteContaining.GetInvoker(t)(parts, count - results.Count));

                    if (results.Count >= count)
                        return results;
                }
            }

            return results;
        }

        public static List<Lite<T>> Autocomplete<T>(this IQueryable<Lite<T>> query, string subString, int count)
            where T : Entity
        {
            using(ExecutionMode.UserInterface())
            {

                List<Lite<T>> results = new List<Lite<T>>();

                PrimaryKey id;
                if (PrimaryKey.TryParse(subString, typeof(T), out id))
                {
                    Lite<T> entity = query.SingleOrDefaultEx(e => e.Id == id);

                    if (entity != null)
                        results.Add(entity);

                    if (results.Count >= count)
                        return results;
                }

                var parts = subString.Trim('\'', '"').SplitNoEmpty(' ');

                results.AddRange(query.Where(a => a.ToString().ContainsAll(parts))
                    .OrderBy(a => a.ToString().Length)
                    .Take(count - results.Count));

                return results;
            }
        }

        public static List<Lite<T>> Autocomplete<T>(this IEnumerable<Lite<T>> query, string subString, int count)
            where T : Entity
        {
            using (ExecutionMode.UserInterface())
            {
                List<Lite<T>> results = new List<Lite<T>>();

                PrimaryKey id;
                if (PrimaryKey.TryParse(subString, typeof(T), out id))
                {
                    Lite<T> entity = query.SingleOrDefaultEx(e => e.Id == id);

                    if (entity != null)
                        results.Add(entity);

                    if (results.Count >= count)
                        return results;
                }

                var parts = subString.Trim('\'', '"').SplitNoEmpty(' ' );

                results.AddRange(query.Where(a =>  a.ToString().ContainsAll(parts))
                    .OrderBy(a => a.ToString().Length)
                    .Take(count - results.Count));

                return results;
            }
        }

        static GenericInvoker<Func<PrimaryKey, Lite<Entity>>> miLiteById =
            new GenericInvoker<Func<PrimaryKey, Lite<Entity>>>(id => LiteById<TypeEntity>(id));
        static Lite<Entity> LiteById<T>(PrimaryKey id)
            where T : Entity
        {
            return Database.Query<T>().Where(a => a.id == id).Select(a => a.ToLite()).SingleOrDefault();
        }

        static GenericInvoker<Func<string[], int, List<Lite<Entity>>>> miLiteContaining =
            new GenericInvoker<Func<string[], int, List<Lite<Entity>>>>((parts, c) => LiteContaining<TypeEntity>(parts, c));
        static List<Lite<Entity>> LiteContaining<T>(string[] parts, int count)
            where T : Entity
        {
            return Database.Query<T>()
                .Where(a => a.ToString().ContainsAll(parts))
                .OrderBy(a => a.ToString().Length)
                .Select(a => a.ToLite())
                .Take(count)
                .AsEnumerable()
                .Cast<Lite<Entity>>()
                .ToList();
        }

        public static List<Lite<Entity>> FindAllLite(Implementations implementations)
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
