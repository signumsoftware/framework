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
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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

        public static async Task<List<Lite<Entity>>> FindLiteLikeAsync(Implementations implementations, string subString, int count, CancellationToken cancellationToken)
        {
            if (implementations.IsByAll)
                throw new InvalidOperationException("ImplementedByAll not supported for FindLiteLike");

            try
            {
                using (ExecutionMode.UserInterface())
                    return await FindLiteLikeAsync(implementations.Types, subString, count, cancellationToken);
            }
            catch (Exception e)
            {
                e.Data["implementations"] = implementations.ToString();
                throw;
            }
        }

        private static bool TryParsePrimaryKey(string value, Type type, out PrimaryKey id)
        {
            var match = Regex.Match(value, "^id[:]?(.*)", RegexOptions.IgnoreCase);
            if (match.Success)
                return PrimaryKey.TryParse(match.Groups[1].ToString(), type, out id);

            id = default(PrimaryKey);
            return false;
        }

        static List<Lite<Entity>> FindLiteLike(IEnumerable<Type> types, string subString, int count)
        {
            if (subString == null)
                subString = "";

            types = types.Where(t => Schema.Current.IsAllowed(t, inUserInterface: true) == null);

            List<Lite<Entity>> results = new List<Lite<Entity>>();
          
            foreach (var t in types)
            {
                if (TryParsePrimaryKey(subString, t, out PrimaryKey id))
                {
                    var lite = giLiteById.GetInvoker(t).Invoke(id);
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
                if (!TryParsePrimaryKey(subString, t, out PrimaryKey id))
                {
                    var parts = subString.Trim().SplitNoEmpty(' ');

                    results.AddRange(giLiteContaining.GetInvoker(t)(parts, count - results.Count));

                    if (results.Count >= count)
                        return results;
                }
            }

            return results;
        }

        private static async Task<List<Lite<Entity>>> FindLiteLikeAsync(IEnumerable<Type> types, string subString, int count, CancellationToken cancellationToken)
        {
            if (subString == null)
                subString = "";

            types = types.Where(t => Schema.Current.IsAllowed(t, inUserInterface: true) == null);

            List<Lite<Entity>> results = new List<Lite<Entity>>();

            foreach (var t in types)
            {
                if (TryParsePrimaryKey(subString, t, out PrimaryKey id))
                {
                    var lite = await giLiteByIdAsync.GetInvoker(t).Invoke(id, cancellationToken);
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
                if (!TryParsePrimaryKey(subString, t, out PrimaryKey id))
                {
                    var parts = subString.Trim().SplitNoEmpty(' ');

                    var list = await giLiteContainingAsync.GetInvoker(t)(parts, count - results.Count, cancellationToken);
                    results.AddRange(list);

                    if (results.Count >= count)
                        return results;
                }
            }

            return results;
        }
     
        static GenericInvoker<Func<PrimaryKey, Lite<Entity>>> giLiteById =
            new GenericInvoker<Func<PrimaryKey, Lite<Entity>>>(id => LiteById<TypeEntity>(id));
        static Lite<Entity> LiteById<T>(PrimaryKey id)
            where T : Entity
        {
            return Database.Query<T>().Where(a => a.id == id).Select(a => a.ToLite()).SingleOrDefault();
        }

        static GenericInvoker<Func<PrimaryKey, CancellationToken, Task<Lite<Entity>>>> giLiteByIdAsync =
            new GenericInvoker<Func<PrimaryKey, CancellationToken, Task<Lite<Entity>>>>((id, token) => LiteByIdAsync<TypeEntity>(id, token));
        static Task<Lite<Entity>> LiteByIdAsync<T>(PrimaryKey id, CancellationToken token)
            where T : Entity
        {
            return Database.Query<T>().Where(a => a.id == id).Select(a => a.ToLite()).SingleOrDefaultAsync(token).ContinueWith(t => (Lite<Entity>)t.Result);
        }

        static GenericInvoker<Func<string[], int, List<Lite<Entity>>>> giLiteContaining =
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

        static GenericInvoker<Func<string[], int, CancellationToken, Task<List<Lite<Entity>>>>> giLiteContainingAsync =
            new GenericInvoker<Func<string[], int, CancellationToken, Task<List<Lite<Entity>>>>>((parts, c, token) => LiteContaining<TypeEntity>(parts, c, token));
        static async Task<List<Lite<Entity>>> LiteContaining<T>(string[] parts, int count, CancellationToken token)
            where T : Entity
        {
            var list = await Database.Query<T>()
                .Where(a => a.ToString().ContainsAll(parts))
                .OrderBy(a => a.ToString().Length)
                .Select(a => a.ToLite())
                .Take(count)
                .ToListAsync(token);

            return list.Cast<Lite<Entity>>().ToList();
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

        public static async Task<List<Lite<Entity>>> FindAllLiteAsync(Implementations implementations, CancellationToken token)
        {
            if (implementations.IsByAll)
                throw new InvalidOperationException("ImplementedByAll is not supported for RetrieveAllLite");

            try
            {
                using (ExecutionMode.UserInterface())
                {
                    var tasks = implementations.Types.Select(type => Database.RetrieveAllLiteAsync(type, token)).ToList();

                    var list = await Task.WhenAll(tasks);

                    return list.SelectMany(li => li).ToList();
                }
            }
            catch (Exception e)
            {
                e.Data["implementations"] = implementations.ToString();
                throw;
            }
        }

        public static List<T> AutocompleteUntyped<T>(this IQueryable<T> query, Expression<Func<T, Lite<Entity>>> entitySelector, string subString, int count, Type type)
        {
            using (ExecutionMode.UserInterface())
            {
                List<T> results = new List<T>();

                if (TryParsePrimaryKey(subString, type, out PrimaryKey id))
                {
                    T entity = query.SingleOrDefaultEx(r => entitySelector.Evaluate(r).Id == id);

                    if (entity != null)
                        results.Add(entity);

                    if (results.Count >= count)
                        return results;
                }

                var parts = subString.Trim().SplitNoEmpty(' ');

                var list = query
                    .Where(r => entitySelector.Evaluate(r).ToString().ContainsAll(parts))
                    .OrderBy(r => entitySelector.Evaluate(r).ToString().Length)
                    .Take(count - results.Count)
                    .ToList();

                results.AddRange(list);

                return results;
            }
        }

        public static async Task<List<T>> AutocompleteUntypedAsync<T>(this IQueryable<T> query, Expression<Func<T, Lite<Entity>>> entitySelector, string subString, int count, Type type, CancellationToken token)
        {
            using (ExecutionMode.UserInterface())
            {
                List<T> results = new List<T>();

                if (TryParsePrimaryKey(subString, type, out PrimaryKey id))
                {
                    T entity = await query.SingleOrDefaultAsync(r => entitySelector.Evaluate(r).Id == id);

                    if (entity != null)
                        results.Add(entity);

                    if (results.Count >= count)
                        return results;
                }

                var parts = subString.Trim().SplitNoEmpty(' ');

                var list = await query.Where(r => entitySelector.Evaluate(r).ToString().ContainsAll(parts))
                    .OrderBy(r => entitySelector.Evaluate(r).ToString().Length)
                    .Take(count - results.Count)
                    .ToListAsync();

                results.AddRange(list);

                return results;
            }
        }

        public static List<Lite<T>> Autocomplete<T>(this IQueryable<Lite<T>> query, string subString, int count)
            where T : Entity
        {
            using (ExecutionMode.UserInterface())
            {

                List<Lite<T>> results = new List<Lite<T>>();

                if (TryParsePrimaryKey(subString, typeof(T), out PrimaryKey id))
                {
                    Lite<T> entity = query.SingleOrDefaultEx(e => e.Id == id);

                    if (entity != null)
                        results.Add(entity);

                    if (results.Count >= count)
                        return results;
                }

                var parts = subString.Trim().SplitNoEmpty(' ');

                results.AddRange(query.Where(a => a.ToString().ContainsAll(parts))
                    .OrderBy(a => a.ToString().Length)
                    .Take(count - results.Count));

                return results;
            }
        }

        public static async Task<List<Lite<T>>> AutocompleteAsync<T>(this IQueryable<Lite<T>> query, string subString, int count, CancellationToken token)
                where T : Entity
        {
            using (ExecutionMode.UserInterface())
            {
                List<Lite<T>> results = new List<Lite<T>>();

                if (TryParsePrimaryKey(subString, typeof(T), out PrimaryKey id))
                {
                    Lite<T> entity = await query.SingleOrDefaultAsync(e => e.Id == id, token);

                    if (entity != null)
                        results.Add(entity);

                    if (results.Count >= count)
                        return results;
                }

                var parts = subString.Trim().SplitNoEmpty(' ');

                var list = await query.Where(a => a.ToString().ContainsAll(parts))
                    .OrderBy(a => a.ToString().Length)
                    .Take(count - results.Count)
                    .ToListAsync(token);

                results.AddRange(list);

                return results;
            }
        }

        public static List<Lite<T>> Autocomplete<T>(this IEnumerable<Lite<T>> collection, string subString, int count)
            where T : Entity
        {
            using (ExecutionMode.UserInterface())
            {
                List<Lite<T>> results = new List<Lite<T>>();

                if (TryParsePrimaryKey(subString, typeof(T), out PrimaryKey id))
                {
                    Lite<T> entity = collection.SingleOrDefaultEx(e => e.Id == id);

                    if (entity != null)
                        results.Add(entity);

                    if (results.Count >= count)
                        return results;
                }

                var parts = subString.Trim().SplitNoEmpty(' ');

                var list = collection.Where(a => a.ToString().ContainsAll(parts))
                    .OrderBy(a => a.ToString().Length)
                    .Take(count - results.Count);

                results.AddRange(list);

                return results;
            }
        }

    }
}
