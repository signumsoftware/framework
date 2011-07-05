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
using Signum.Engine.Maps;
using System.Collections;
using Signum.Engine.Linq;
using Signum.Utilities.Reflection;
using System.Linq.Expressions;

namespace Signum.Engine
{
    public class EntityCache: IDisposable
    {
        internal class RealEntityCache : Dictionary<IdentityTuple, IdentifiableEntity>
        {
            public void Add(IdentifiableEntity ie)
            {
                if (ie == null)
                    throw new ArgumentNullException("ie");

                var tuple = new IdentityTuple(ie);

                IdentifiableEntity ident = this.TryGetC(tuple);

                if (ident == null)
                    this.Add(tuple, ie);
                else if (ident != ie)
                   throw new InvalidOperationException("There's a different instance of the same entity with Type '{0}' and Id '{1}'".Formato(ie.GetType().Name, ie.id));
            }

            public bool Contains(Type type, int id)
            {
                return this.ContainsKey(new IdentityTuple(type, id));
            }

            public IdentifiableEntity Get(Type type, int id)
            {
                return this.TryGetC(new IdentityTuple(type, id));
            }

            public void AddFullGraph(IdentifiableEntity ie)
            {
                DirectedGraph<Modifiable> modifiables = GraphExplorer.FromRoot(ie);

                foreach (var ident in modifiables.OfType<IdentifiableEntity>().Where(ident => !ident.IsNew))
                    Add(ident);
            }

            IRetriever Retriever { get; set; }

            internal IRetriever NewRetriever()
            {
                if (Retriever == null)
                    Retriever = new RealRetriever(this);
                else
                    Retriever = new ChildRetriever(Retriever, this);

                return Retriever;
            }

            internal void ReleaseRetriever(IRetriever retriever)
            {
                if (retriever == null || retriever != Retriever)
                    throw new InvalidOperationException("Inconsistent state of the retriever");

                Retriever = retriever.Parent;
            }
        }


        [ThreadStatic]
        private static ImmutableStack<RealEntityCache> stack;

        private bool facked = false;

        public EntityCache() : this(false) { }

        public EntityCache(bool forceNew)
        {
            if (stack == null)
                stack = ImmutableStack<RealEntityCache>.Empty;

            if (stack.IsEmpty || forceNew)
                stack = stack.Push(new RealEntityCache());
            else
                facked = true;
        }

        static RealEntityCache Current
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

        internal static IRetriever NewRetriever()
        {
            return Current.NewRetriever();
        }
    
        internal static void ReleaseRetriever(IRetriever retriever)
        {
            Current.ReleaseRetriever(retriever);
        }
    
    }

    [Serializable]
    internal struct IdentityTuple : IEquatable<IdentityTuple>
    {
        public readonly Type Type;
        public readonly int Id;

        public IdentityTuple(Lite lite)
        {
            this.Type = lite.RuntimeType;
            this.Id = lite.Id;
        }

        public IdentityTuple(IdentifiableEntity entiy)
        {
            this.Type = entiy.GetType();
            this.Id = entiy.Id;
        }

        public IdentityTuple(Type type, int id)
        {
            this.Type = type;
            this.Id = id;
        }

        public override int GetHashCode()
        {
            return this.Type.GetHashCode() ^ this.Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals((IdentityTuple)obj);
        }

        public override string ToString()
        {
            return "[{0},{1}]".Formato(this.Type.Name, this.Id);
        }

        public bool Equals(IdentityTuple other)
        {
            return Id == other.Id && this.Type == other.Type;
        }
    }


}
