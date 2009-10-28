using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Entities.Properties;
using System.Reflection;
using Signum.Utilities.ExpressionTrees;
using System.Linq.Expressions;

namespace Signum.Entities
{
    [Serializable]
    public class Lite<T> : Lite
        where T : class, IIdentifiable
    {
        T entityOrNull;

        // Methods
        protected Lite()
        {
        }

        public Lite(int id)
            : base(typeof(T), id)
        {
        }

        public Lite(Type runtimeType, int id)
            : base(runtimeType, id)
        {
            if (!typeof(T).IsAssignableFrom(runtimeType))
                throw new ApplicationException(Resources.TypeIsNotSmallerThan.Formato(runtimeType, typeof(T)));
        }

        internal Lite(T entidad)
            : base((IdentifiableEntity)(IIdentifiable)entidad)
        {
        }

        public override IdentifiableEntity UntypedEntityOrNull
        {
            get { return (IdentifiableEntity)(object)EntityOrNull; }
            internal set { EntityOrNull = (T)(object)value; }
        }

        public T EntityOrNull
        {
            get { return entityOrNull; }
            internal set { entityOrNull = value; }
        }
    }

    [Serializable]
    public abstract class Lite : Modifiable
    {
        Type runtimeType;
        int? id;
        string toStr;

        protected Lite()
        {
        }

        protected Lite(Type runtimeType, int id)
        {
            if (runtimeType == null || !typeof(IdentifiableEntity).IsAssignableFrom(runtimeType))
                throw new ApplicationException(Resources.TypeIsNotSmallerThan.Formato(runtimeType, typeof(IIdentifiable)));

            this.runtimeType = runtimeType;
            this.id = id;
        }

        protected Lite(IdentifiableEntity entidad)
        {
            if (entidad == null)
                throw new ArgumentNullException("entidad");

            this.runtimeType = entidad.GetType();
            this.UntypedEntityOrNull = entidad;
            this.id = entidad.IdOrNull;
        }

        public int RefreshId()
        {
            if (UntypedEntityOrNull != null)
                id = UntypedEntityOrNull.Id;
            return id.Value;
        }

        public Type RuntimeType
        {
            get { return runtimeType; }
        }

        public int Id
        {
            get
            {
                if (id == null)
                    throw new ApplicationException(Resources.TheLiteIsPointingToANewEntityAndHasNoIdYet);
                return id.Value;
            }
        }

        public int? IdOrNull
        {
            get { return id; }
        }

        public abstract IdentifiableEntity UntypedEntityOrNull
        {
            get;
            internal set;
        }

        public void SetEntity(IdentifiableEntity ei)
        {
            if (id == null)
                throw new ApplicationException(Resources.NewEntitiesAreNotAllowed);

            if (id != ei.id || RuntimeType != ei.GetType())
                throw new ApplicationException(Resources.EntitiesDoNotMatch);

            this.UntypedEntityOrNull = ei;
        }

        public void ClearEntity()
        {
            if (id == null)
                throw new ApplicationException(Resources.RemovingEntityNotAllowedInNewLazies);

            this.UntypedEntityOrNull = null;
        }

        protected internal override void PreSaving(ref bool graphModified)
        {
            if (UntypedEntityOrNull != null)
            {
                UntypedEntityOrNull.PreSaving(ref graphModified);
                toStr = UntypedEntityOrNull.ToStr;
            }
            //Is better to have an old string than having nothing
        }

        public override bool SelfModified
        {
            get { return false; }
            internal set { }
        }

        public override string ToString()
        {
            if (this.UntypedEntityOrNull != null)
                return this.UntypedEntityOrNull.ToString();
            if (this.toStr != null)
                return this.toStr;
            return "{0}({1})".Formato(this.RuntimeType, this.id);
        }

        public string ToStringLong()
        {
            if (this.UntypedEntityOrNull == null)
                return "[({0}:{1}) ToStr:{2}]".Formato(this.runtimeType.Name, this.id, this.toStr);
            return "[{0}]".Formato(this.UntypedEntityOrNull);
        }

        public static Lite Create(Type type, int id)
        {
            return (Lite)Activator.CreateInstance(Reflector.GenerateLite(type), type, id);
        }

        public static Lite Create(Type type, int id, Type runtimeType)
        {
            return (Lite)Activator.CreateInstance(Reflector.GenerateLite(type), runtimeType, id);
        }

        public static Lite Create(Type type, int id, Type runtimeType, string toStr)
        {
            Lite result = (Lite)Activator.CreateInstance(Reflector.GenerateLite(type), runtimeType, id);
            result.ToStr = toStr;
            return result;
        }

        public static Lite Create(Type type, IdentifiableEntity entidad)
        {
            if (entidad == null)
                throw new ArgumentNullException("entidad");

            BindingFlags bf = BindingFlags.Default | BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.NonPublic;

            ConstructorInfo ci = Reflector.GenerateLite(type).GetConstructor(bf, null, new[] { type }, null);

            Lite result = (Lite)ci.Invoke(new[] { entidad });
            result.ToStr = entidad.TryToString();
            return result;
        }

        public string ToStr
        {
            get { return toStr; }
            internal set { toStr = value; }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (this == obj)
                return true;

            Lite lazy = obj as Lite;
            if (lazy != null)
            {
                if (RuntimeType != lazy.RuntimeType)
                    return false;

                if (UntypedEntityOrNull == null)
                    return Id == lazy.IdOrNull;
                else
                    return object.ReferenceEquals(this.UntypedEntityOrNull, lazy.UntypedEntityOrNull);
            }

            return false;
        }

        const int MagicMask = 123456853;
        public override int GetHashCode()
        {
            if (this.UntypedEntityOrNull != null)
                return UntypedEntityOrNull.GetHashCode() ^ MagicMask;
            return this.Id.GetHashCode() ^ this.RuntimeType.Name.GetHashCode() ^ MagicMask;
        }
    }


    public static class LiteUtils
    {
        public static Lite<T> ToLite<T>(this Lite lazy)
            where T : class, IIdentifiable
        {
            if (lazy == null)
                return null;

            if (lazy is Lite<T>)
                return (Lite<T>)lazy;

            if (lazy.UntypedEntityOrNull != null)
                return new Lite<T>((T)(object)lazy.UntypedEntityOrNull) { ToStr = lazy.ToStr };
            else
                return new Lite<T>(lazy.RuntimeType, lazy.Id) { ToStr = lazy.ToStr };
        }

        public static Lite<T> ToLite<T>(this Lite lazy, string toStr)
            where T : class, IIdentifiable
        {
            if (lazy == null)
                return null;

            if (lazy is Lite<T>)
                return (Lite<T>)lazy;

            if (lazy.UntypedEntityOrNull != null)
                return new Lite<T>((T)(object)lazy.UntypedEntityOrNull) { ToStr = toStr };
            else
                return new Lite<T>(lazy.RuntimeType, lazy.Id) { ToStr = toStr };
        }

        public static Lite<T> ToLite<T>(this T entity)
          where T : class, IIdentifiable
        {
            if (entity == null)
                return null;


            if (entity.IsNew)
                throw new ApplicationException(Resources.ToLiteLightNotAllowedForNewEntities);

            return new Lite<T>(entity.GetType(), entity.Id) { ToStr = entity.ToString() };
        }

        public static Lite<T> ToLite<T>(this T entity, string toStr)
            where T : class, IIdentifiable
        {
            if (entity == null)
                return null;

            if (entity.IsNew)
                throw new ApplicationException(Resources.ToLiteLightNotAllowedForNewEntities);

            return new Lite<T>(entity.GetType(), entity.Id) { ToStr = toStr };
        }

        public static Lite<T> ToLite<T>(this T entity, bool fat) where T : class, IIdentifiable
        {
            if (fat)
                return entity.ToLiteFat();
            else
                return entity.ToLite();
        }

        public static Lite<T> ToLite<T>(this T entity, bool fat, string toStr) where T : class, IIdentifiable
        {
            if (fat)
                return entity.ToLiteFat(toStr);
            else
                return entity.ToLite(toStr);
        }

        public static Lite<T> ToLiteFat<T>(this T entity) where T : class, IIdentifiable
        {
            if (entity == null)
                return null;

            return new Lite<T>(entity) { ToStr = entity.ToString() };
        }

        public static Lite<T> ToLiteFat<T>(this T entity, string toStr) where T : class, IIdentifiable
        {
            if (entity == null)
                return null;

            return new Lite<T>(entity) { ToStr = toStr };
        }

        [MethodExpander(typeof(RefersToExpander))]
        public static bool RefersTo<T>(this Lite<T> lazy, T entity)
            where T : class, IIdentifiable
        {
            if (lazy == null && entity == null)
                return true;

            if (entity == null || entity == null)
                return false;

            if (lazy.RuntimeType != entity.GetType())
                return false;

            if (lazy.IdOrNull != null)
                return lazy.Id == entity.IdOrNull;
            else
                return object.ReferenceEquals(lazy.EntityOrNull, entity);
        }

        class RefersToExpander : IMethodExpander
        {
            public Expression Expand(Expression instance, Expression[] arguments)
            {
                Expression lazy = arguments[0];
                return Expression.Equal(Expression.MakeMemberAccess(lazy, lazy.Type.GetProperty("EntityOrNull", BindingFlags.Instance| BindingFlags.Public)), arguments[1]);
            }
        }

        [MethodExpander(typeof(IsExpander))]
        public static bool Is<T>(this T entity1, T entity2)
             where T : class, IIdentifiable
        {
            if (entity1 == null && entity2 == null)
                return true;

            if (entity1 == null || entity2 == null)
                return false;

            if (entity1.GetType() != entity2.GetType())
                return false;

            if (entity1.IdOrNull != null)
                return entity1.Id == entity2.IdOrNull;
            else
                return object.ReferenceEquals(entity1, entity2);
        }

        class IsExpander : IMethodExpander
        {
            public Expression Expand(Expression instance, Expression[] arguments)
            {
                return Expression.Equal(arguments[0], arguments[1]);
            }
        }

        [MethodExpander(typeof(IsExpander))]
        public static bool Is<T>(this Lite<T> lazy1, Lite<T> lazy2)
            where T : class, IIdentifiable
        {
            if (lazy1 == null && lazy2 == null)
                return true;

            if (lazy1 == null || lazy2 == null)
                return false;

            if (lazy1.GetType() != lazy2.GetType())
                return false;

            if (lazy1.IdOrNull != null && lazy2.IdOrNull != null)
                return lazy1.Id == lazy2.Id;
            else
                return object.ReferenceEquals(lazy1.EntityOrNull, lazy2.EntityOrNull);
        }
    }
}
