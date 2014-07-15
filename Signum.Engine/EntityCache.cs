using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Engine;
using Signum.Entities.Reflection;
using Signum.Engine.Maps;
using System.Collections;
using Signum.Engine.Linq;
using Signum.Utilities.Reflection;
using System.Linq.Expressions;

namespace Signum.Engine
{
    public class EntityCache: IDisposable
    {
        internal class RealEntityCache
        {
            readonly Dictionary<IdentityTuple, IdentifiableEntity> dic = new Dictionary<IdentityTuple,IdentifiableEntity>();

            public void Add(IdentifiableEntity ie)
            {
                if (ie == null)
                    throw new ArgumentNullException("ie");

                var tuple = new IdentityTuple(ie);

                IdentifiableEntity ident = dic.TryGetC(tuple);

                if (ident == null)
                    dic.Add(tuple, ie);
                else if (ident != ie)
                {
                    //Odd but allowed
                    //throw new InvalidOperationException("There's a different instance of the same entity with Type '{0}' and Id '{1}'".Formato(ie.GetType().Name, ie.id));
                }
            }

            public bool Contains(Type type, int id)
            {
                return dic.ContainsKey(new IdentityTuple(type, id));
            }

            public IdentifiableEntity Get(Type type, int id)
            {
                return dic.TryGetC(new IdentityTuple(type, id));
            }

            public void AddFullGraph(ModifiableEntity ie)
            {
                DirectedGraph<Modifiable> modifiables = GraphExplorer.FromRoot(ie);

                foreach (var ident in modifiables.OfType<IdentifiableEntity>().Where(ident => !ident.IsNew))
                    Add(ident);
            }

            IRetriever retriever;
            public RealEntityCache(bool isSealed)
            {
                IsSealed = isSealed;
            }

            public IRetriever NewRetriever()
            {
                if (retriever == null)
                    retriever = new RealRetriever(this);
                else
                    retriever = new ChildRetriever(retriever, this);

                return retriever;
            }

            internal void ReleaseRetriever(IRetriever retriever)
            {
                if (this.retriever == null || this.retriever != retriever)
                    throw new InvalidOperationException("Inconsistent state of the retriever");

                this.retriever = retriever.Parent;
            }

            internal bool HasRetriever
            {
                get{return retriever != null; }
            }

            internal bool TryGetValue(IdentityTuple tuple, out IdentifiableEntity result)
            {
                return dic.TryGetValue(tuple, out result);
            }

            public bool IsSealed { get; private set; }
        }


        static readonly Variable<RealEntityCache> currentCache = Statics.ThreadVariable<RealEntityCache>("cache");


        RealEntityCache oldCache;

        private bool facked = false;

        public EntityCache(EntityCacheType type = EntityCacheType.Normal)
        {
            if (currentCache.Value == null || type != EntityCacheType.Normal)
            {
                oldCache = currentCache.Value;
                currentCache.Value = new RealEntityCache(type == EntityCacheType.ForceNewSealed);
            }
            else
                facked = true;
        }

        static RealEntityCache Current
        {
            get
            {
                var val = currentCache.Value;
                if (val == null)
                    throw new InvalidOperationException("No EntityCache context has been created");

                return val;
            }
        }

        public static bool Created { get { return currentCache.Value != null; } }

        internal static bool HasRetriever { get { return currentCache.Value != null && currentCache.Value.HasRetriever; } }

        public static bool IsSealed { get { return currentCache.Value.IsSealed; } }

        public void Dispose()
        {
            if (!facked)
                currentCache.Value = oldCache;
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

        public static void AddFullGraph(ModifiableEntity ie)
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

        public static IRetriever NewRetriever()
        {
            return Current.NewRetriever();
        }
    
        internal static void ReleaseRetriever(IRetriever retriever)
        {
            Current.ReleaseRetriever(retriever);
        }

        public static T Construct<T>(int id) where T : IdentifiableEntity
        {
            var result = Constructor<T>.Call();
            result.id = id;
            return result;
        }

        static class Constructor<T> where T : IdentifiableEntity
        {
            static Func<T> call;
            public static Func<T> Call
            {
                get
                {
                    if (call == null)
                        call = Expression.Lambda<Func<T>>(Expression.New(typeof(T))).Compile();
                    return call;
                }
            }
        }
    }

    public enum EntityCacheType
    {
        Normal,
        ForceNew,
        ForceNewSealed
    }

    [Serializable]
    internal struct IdentityTuple : IEquatable<IdentityTuple>
    {
        public readonly Type Type;
        public readonly int Id;

        public IdentityTuple(Lite<IdentifiableEntity> lite)
        {
            this.Type = lite.EntityType;
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
