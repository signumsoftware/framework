using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Engine;
using Signum.Entities.Reflection;
using Signum.Engine.Properties;

namespace Signum.Engine
{
    public class EntityCache: IDisposable
    {
        class RealObjectCache : Dictionary<Type, Dictionary<int, IdentifiableEntity>>
        {
            public void Add(IdentifiableEntity ie)
            {
                if (ie == null)
                    throw new ArgumentNullException("ie");

                var dic = this.GetOrCreate(ie.GetType());

                IdentifiableEntity ident = dic.TryGetC(ie.Id);

                if (ident == null)
                    dic.Add(ie.Id, ie);
                else if (ident != ie)
                    throw new InvalidOperationException("There's a different instance of the same entity with Type '{0}' and Id '{1}'".Formato(ie.GetType().Name, ie.id));
            }

            public bool Contains(Type type, int id)
            {
                var dic = this.TryGetC(type);
                if (dic == null)
                    return false;

                return dic.ContainsKey(id);
            }

            public IdentifiableEntity Get(Type type, int id)
            {
                return this.TryGetC(type).TryGetC(id);
            }

            public void AddFullGraph(IdentifiableEntity ie)
            {
                DirectedGraph<Modifiable> modifiables = GraphExplorer.FromRoot(ie);

                foreach (var ident in modifiables.OfType<IdentifiableEntity>().Where(ident => !ident.IsNew))
                    if (Get(ident.GetType(), ident.Id) != ident)
                        Add(ident);
            }
        }


        [ThreadStatic]
        private static ImmutableStack<RealObjectCache> stack;

        private bool facked = false;

        public EntityCache() : this(false) { }

        public EntityCache(bool forceNew)
        {
            if (stack == null)
                stack = ImmutableStack<RealObjectCache>.Empty;

            if (stack.IsEmpty || forceNew)
                stack = stack.Push(new RealObjectCache());
            else
                facked = true;
        }

        static RealObjectCache Current
        {
            get
            {
                if (stack.IsEmpty)
                    throw new InvalidOperationException("No EntityCache context has been created");

                return stack.Peek(); 
            }
        }


        public void Dispose()
        {
            if (!facked)
                stack = stack.Pop();
        }

        public static void AddMany<T>(params T[] objects)
            where T: IdentifiableEntity
        {
            foreach (var item in objects)
                Add(item);
        }

        public static void Add<T>(IEnumerable<T> objects)
            where T: IdentifiableEntity
        {
            foreach (var item in objects)
                Add(item);
        }

        public static void AddFullGraph(IdentifiableEntity ie)
        {
            Current.AddFullGraph(ie);
        }

        public static void Add(IdentifiableEntity ie)
        {
            Current.Add(ie);
        }


        public static bool Contains<T>(int id) where T : IdentifiableEntity
        {
            return Contains(typeof(T), id);
        }

        public static bool Contains(Type type, int id)
        {
            return Current.Contains(type, id); 
        }

        public static T Get<T>(int id) where T : IdentifiableEntity
        {
            return (T)Get(typeof(T), id);
        }

        public static IdentifiableEntity Get(Type type, int id)
        {
            return Current.Get(type, id);
        }
    }
}
